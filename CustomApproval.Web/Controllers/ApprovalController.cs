using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CustomApproval.Web.Entities;
using CustomApproval.Web.Extensions;
using CustomApproval.Web.Models;
using CustomApproval.Web.Models.GraphApi;
using CustomApproval.Web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace CustomApproval.Web.Controllers
{
    public class ApprovalController : Controller
    {

        private readonly IUserService userService;
        private readonly IGraphSdkClientService graphService;
        private readonly IMailService mailService;
        private readonly GraphSettings graphSettings;
        private readonly AppSettings appSettings;

        public ApprovalController(
            IConfiguration config,
            IUserService userService,
            IGraphSdkClientService graphService,
            IMailService mailService)
        {
            this.userService = userService;
            this.graphService = graphService;
            this.mailService = mailService;

            graphSettings = config.GetSection("GraphApi")
              .Get<GraphSettings>();
            appSettings = config.GetSection("AppSettings")
              .Get<AppSettings>();
        }

        // GET: Approval
        public ActionResult Index()
        {

            return View();
        }

        [HttpPost]
        public async Task<ActionResult> Details(FindRequestModel data)
        {
            ViewBag.Email = data.Email;

            var users = await userService.GetUsersByEmail(data.Email);
            var groups = await this.graphService.GetGroups();

            var detailsModel = new DetailsModel()
            {
                Users = users.ToUserModel(),
                Groups = groups
            };

            return View(detailsModel);
        }

        [HttpPost]
        public async Task<ActionResult> ApproveRequest(string internalUserId, string[] SelectedGroups)
        {
            var user = await userService.GetUsersById(internalUserId);
            ViewBag.Message = "An error occurred while updating the request.";

            if (user == null)
            {
                return View("Index");
            }

            string graphUserId;
            if (user.IsSocialUser())
            {
                var createUserObj = user.SdkToSocialUserInput(graphSettings.Tenant);
                var createdUser = await graphService.CreateUser(createUserObj);
                graphUserId = createdUser.Id;

                await mailService.SendApprovalNotification(user.Email, user.Locale);
            }
            else
            {
                var result = await graphService.InviteGuestUser(user.Email);
                if (result == null || string.IsNullOrEmpty(result.invitedUser?.id))
                {
                    return View("Index");
                }

                graphUserId = result.invitedUser.id;
                var updateUserObj = user.SdkToUpdateGuestUserInput();
                await graphService.UpdateUser(graphUserId, updateUserObj);
            }

            await graphService.AddUserToGroups(graphUserId, SelectedGroups);
            await userService.RemoveUsersById(internalUserId);

            ViewBag.Message = "Update was successful.";
            return View("Index");
        }

        [HttpPost]
        public async Task<ActionResult> DenyRequest(string Id)
        {
            await userService.UpdateUserStatusAsync(Id, Constants.UserApprovalStatus.Denied);

            ViewBag.Message = "Update was successful.";

            return View("Index");
        }
    }
}