using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WorkFlowBasic.Models;
using WorkFlowBasic.Services;

namespace WorkFlowBasic.Controllers;

[Authorize]
public class ChatController : Controller
{
    private readonly ChatBotService _chatService;
    private readonly UserManager<ApplicationUser> _userManager;

    public ChatController(ChatBotService chatService, UserManager<ApplicationUser> userManager)
    {
        _chatService = chatService;
        _userManager = userManager;
    }

    [HttpPost]
    public async Task<IActionResult> SendMessage(string message)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        // Servise sor ve cevabı al
        var response = await _chatService.GetResponse(message, user.Id);

        return Json(new { reply = response });
    }
}