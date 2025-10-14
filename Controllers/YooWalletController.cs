using Microsoft.AspNetCore.Mvc;
using OlegBot.Bus;
using OlegBot.Helpers;

[ApiController]
public class YooWalletController : Controller
{
    private readonly IConfiguration _config;

    public YooWalletController(IConfiguration config)
    {
        _config = config;
    }

    [HttpGet("redirect")]
    public async Task<IActionResult> HandleCallback([FromQuery] string code, [FromQuery] string state)
    {
        var walletHelper = new YooWalletHelper(
            _config["UMoney:ClientId"],
            _config["UMoney:PrivateToken"],
            _config["UMoney:ReturnUrl"]
        );

        try
        {
            var token = await walletHelper.GetAccessToken(code, state);

            AuthEventBus.Notify(state, token);

            return View("Auth");
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}