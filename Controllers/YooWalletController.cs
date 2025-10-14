using Microsoft.AspNetCore.Mvc;
using OlegBot.Bus;
using OlegBot.Helpers;

[ApiController]
public class YooWalletController : ControllerBase
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

            return Ok(
                new
                {
                    success = true,
                    access_token = token,
                    state
                }
            );
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}