using CustomApproval.Web.Models;
using CustomApproval.Web.Models.GraphApi;
using Microsoft.Graph;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CustomApproval.Web.Services
{
    public interface IGraphSdkClientService
    {
        Task<User> CreateUser(User user);
        Task<InviteGuestUserOutputModel> InviteGuestUser(string emailAddress);
        Task UpdateUser(string targetId, User newUser);
        Task<IEnumerable<GroupsModel>> GetGroups();
        Task AddUserToGroups(string id, IEnumerable<string> groups);
    }
}