using CustomApproval.Web.Models.GraphApi;
using Microsoft.Graph;
using System.Threading.Tasks;

namespace CustomApproval.Web.Services
{
    public interface IGraphSdkClientService
    {
        Task CreateUser(User user);
        Task<InviteGuestUserOutputModel> InviteGuestUser(string emailAddress);
        Task UpdateUser(string targetId, User newUser);
    }
}