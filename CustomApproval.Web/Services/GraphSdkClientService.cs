﻿using CustomApproval.Web.Models;
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
            this.graphSettings = config.GetSection("GraphApi").Get<GraphSettings>();
            this.appSettings = config.GetSection("AppSettings").Get<AppSettings>();
            this.graphClient = this.CreateGraphClient(graphSettings);
        }

        public async Task<User> CreateUser(User user)
        {
            return await this.graphClient.Users.Request().AddAsync(user);
        }

        public async Task<InviteGuestUserOutputModel> InviteGuestUser(string emailAddress)
        {
            var invitation = new Invitation()
            {
                InvitedUserEmailAddress = emailAddress,
                InviteRedirectUrl = this.appSettings.ParentAppRedirectUrl,
                SendInvitationMessage = true
            };
            var inviteResponse = await this.graphClient.Invitations
                .Request()
                .AddAsync(invitation);

            return new InviteGuestUserOutputModel()
            {
                invitedUser = new InvitedUser()
                {
                    id = inviteResponse.InvitedUser.Id
                }
            };
        }

        public async Task<IEnumerable<GroupsModel>> GetGroups()
        {
            var groupsApiResponse = await this.graphClient.Groups
                .Request()
                .GetAsync();
            var groupsList = new List<GroupsModel>();
            var pageIterator = PageIterator<Group>
                .CreatePageIterator(graphClient, groupsApiResponse, (g) => {
                    groupsList.Add(new GroupsModel()
                    {
                        DisplayName = g.DisplayName,
                        Id = g.Id
                    });
                    return true;
                });
            await pageIterator.IterateAsync();

            return groupsList;
        }

        public async Task AddUserToGroups(string id, IEnumerable<string> groups)
        {
            var directoryObject = new DirectoryObject
            {
                Id = id
            };

            foreach (string groupId in groups)
            {
                await this.graphClient.Groups[groupId].Members.References
                    .Request()
                    .AddAsync(directoryObject);
            }
        }

        public async Task UpdateUser(string targetId, User newUser)
        {
            var user = await this.graphClient.Users[targetId]
                .Request()
                .UpdateAsync(newUser);
        }

        private GraphServiceClient CreateGraphClient(GraphSettings graphSettings)
        {
            var app = ConfidentialClientApplicationBuilder
                      .Create(graphSettings.ClientId)
                      .WithClientSecret(graphSettings.ClientSecret)
                      .WithAuthority(new Uri(graphSettings.Authority))
                      .Build();

            ClientCredentialProvider authProvider = new ClientCredentialProvider(app);

            return new GraphServiceClient(authProvider);
        }
    }
}
