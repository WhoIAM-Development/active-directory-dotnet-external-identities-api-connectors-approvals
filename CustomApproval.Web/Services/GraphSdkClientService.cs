using CustomApproval.Web.Models;
using CustomApproval.Web.Models.GraphApi;
using Microsoft.Extensions.Configuration;
using Microsoft.Graph;
using Microsoft.Graph.Auth;
using Microsoft.Identity.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CustomApproval.Web.Services
{
    public class GraphSdkClientService : IGraphSdkClientService
    {

        private readonly GraphSettings graphSettings;
        private AppSettings appSettings;
        private readonly GraphServiceClient graphClient;

        public GraphSdkClientService(IConfiguration config)
        {
            this.graphSettings = config.GetSection("GraphApi")
              .Get<GraphSettings>();

            this.appSettings = config.GetSection("AppSettings")
              .Get<AppSettings>();

            var app = ConfidentialClientApplicationBuilder
                      .Create(graphSettings.ClientId)
                      .WithClientSecret(graphSettings.ClientSecret)
                      .WithAuthority(new Uri(graphSettings.Authority))
                      .Build();

            ClientCredentialProvider authProvider = new ClientCredentialProvider(app);

            this.graphClient = new GraphServiceClient(authProvider);
        }

        public async Task CreateUser(User user)
        {
            await this.graphClient.Users.Request().AddAsync(user);
        }

        public async Task<InviteGuestUserOutputModel> InviteGuestUser(User user)
        {
            string email = string.Empty;
            try
            {
                email = user.Identities.First(oid => oid.SignInType == "email").IssuerAssignedId;
            }
            catch (InvalidOperationException ex)
            {
                email = user.Mail;
            }


            var invitation = new Invitation()
            {
                InvitedUser = user,
                InvitedUserEmailAddress = email,
                InviteRedirectUrl = this.appSettings.ParentAppRedirectUrl
            };

            var inviteResponse = await this.graphClient.Invitations
                .Request()
                .AddAsync(invitation);

            return new InviteGuestUserOutputModel()
            {
                invitedUser = new InvitedUser()
                {
                    id = inviteResponse.Id
                }
            };
        }

        public async Task UpdateUser(string targetId, User newUser)
        {
            var user = await this.graphClient.Users[targetId]
                .Request()
                .UpdateAsync(newUser);
        }
    }
}
