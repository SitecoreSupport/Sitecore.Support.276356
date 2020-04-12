﻿using Sitecore.Commerce.Services;
using Sitecore.Commerce.Services.Customers;
using Sitecore.Commerce.XA.Foundation.Common.Context;
using Sitecore.Commerce.XA.Foundation.Common.Models;
using Sitecore.Commerce.XA.Foundation.Common.Utils;
using Sitecore.Commerce.XA.Foundation.Connect.Managers;
using Sitecore.Commerce.XA.Foundation.Connect.Providers;
using Sitecore.Diagnostics;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Security;

namespace Sitecore.Support.Commerce.XA.Foundation.Connect.Managers
{
    public class AccountManager : Sitecore.Commerce.XA.Foundation.Connect.Managers.AccountManager
    {
        public AccountManager([NotNull] IConnectServiceProvider connectServiceProvider, ICartManager cartManager, [NotNull] IStorefrontContext storefrontContext, [NotNull] IModelProvider modelProvider)
        : base(connectServiceProvider, cartManager, storefrontContext, modelProvider)
        {
        }

        public override ManagerResponse<UpdatePasswordResult, bool> ResetUserPassword(string emailAddress, string emailSubject, string emailBody)
        {
            Assert.ArgumentNotNullOrEmpty(emailAddress, "emailAddress");
            Assert.ArgumentNotNullOrEmpty(emailBody, "emailBody");

            var emailSent = false;
            var result = new UpdatePasswordResult { Success = true };

            try
            {
                var getUserResponse = this.GetUser(emailAddress);
                

                if (!getUserResponse.ServiceProviderResult.Success || getUserResponse.Result == null)
                {
                    result.Success = false;

                    foreach (var systemMessage in getUserResponse.ServiceProviderResult.SystemMessages)
                    {
                        result.SystemMessages.Add(systemMessage);
                    }
                }
                else
                {
                    var userIpAddress = HttpContext.Current != null ? HttpContext.Current.Request.UserHostAddress : string.Empty;
                    var userName = Membership.Provider.GetUserNameByEmail(getUserResponse.Result.Email);
                    string provisionalPassword = Membership.Provider.ResetPassword(userName, string.Empty);

                    var mailUtil = new MailUtility();
                    var placeholders = new Hashtable();
                    placeholders.Add("[IPAddress]", userIpAddress);
                    placeholders.Add("[Password]", provisionalPassword);

                    var mailTemplate = this.ModelProvider.GetModel<MailTemplate>();
                    mailTemplate.Initialize(emailSubject, emailBody, emailAddress, placeholders);

                    var wasEmailSent = mailUtil.SendMail(mailTemplate);

                    if (wasEmailSent)
                    {
                        emailSent = true;
                    }
                    else
                    {
                        // var message = StorefrontManager.GetSystemMessage(StorefrontConstants.SystemMessages.CouldNotSentEmailError);
                        // result.SystemMessages.Add(new SystemMessage { Message = message });
                    }
                }
            }
            catch (Exception e)
            {
                result = new UpdatePasswordResult { Success = false };
                result.SystemMessages.Add(new SystemMessage { Message = e.Message });
            }

            return new ManagerResponse<UpdatePasswordResult, bool>(result, emailSent);
        }

    }
}
