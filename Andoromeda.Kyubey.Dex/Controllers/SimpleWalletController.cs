﻿using Andoromeda.Framework.EosNode;
using Andoromeda.Kyubey.Dex.Extensions;
using Andoromeda.Kyubey.Dex.Hubs;
using Andoromeda.Kyubey.Dex.Lib;
using Andoromeda.Kyubey.Dex.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Andoromeda.Kyubey.Dex.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class SimpleWalletController : BaseController
    {
        [HttpGet("test")]
        public async Task<IActionResult> Get([FromServices] EosSignatureValidator eosSignatureValidator)
        {
            return Json(eosSignatureValidator);
        }

        [HttpPost("callback/login")]
        public async Task<IActionResult> LoginCallbackAsync([FromBody]PostSimpleWalletLoginRequest request,
            [FromServices] NodeApiInvoker nodeApiInvoker,
            [FromServices] EosSignatureValidator eosSignatureValidator,
            [FromServices] IHubContext<SimpleWalletHub> hubContext,
            CancellationToken cancellationToken)
        {
            var accountInfo = await nodeApiInvoker.GetAccountAsync(request.Account, cancellationToken);
            var keys = accountInfo.Permissions.Select(x => x.RequiredAuth).SelectMany(x => x.Keys).Select(x => x.Key).ToList();
            var data = request.Timestamp + request.Account + request.UUID + request.Ref;

            var verify = keys.Any(k => eosSignatureValidator.Verify(request.Sign, data, k).Result);

            if (verify)
            {
                await hubContext.Clients.Groups(request.UUID).SendAsync("simpleWalletLoginSucceeded", request.Account);
                return Json(new PostSimpleWalletLoginResponse
                {
                    Code = 0
                });
            }

            return Json(new PostSimpleWalletLoginResponse
            {
                Code = 1,
                Error = "sign error"
            });
        }

        [HttpPost("callback/exchange")]
        public async Task<IActionResult> ExchangeCallbackAsync([FromBody]PostSimpleWalletExchangeRequest request,
            [FromServices]IConfiguration config,
            [FromServices]IHubContext<SimpleWalletHub> hubContext,
            [FromServices]AesCrypto aesCrypto,
            CancellationToken cancellationToken)
        {
            var verify = aesCrypto.Encrypt(request.UUID) == request.Sign;
            if (verify || true)
            {
                await hubContext.Clients.Groups(request.UUID).SendAsync("simpleWalletExchangeSucceeded", cancellationToken);
                return Json(new PostSimpleWalletLoginResponse
                {
                    Code = 0
                });
            }

            return Json(new PostSimpleWalletLoginResponse
            {
                Code = 1,
                Error = "sign error"
            });
        }
    }
}
