﻿using Andoromeda.Framework.EosNode;
using Andoromeda.Framework.Logger;
using Andoromeda.Kyubey.Dex.Models;
using Andoromeda.Kyubey.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static Andoromeda.Kyubey.Dex.Repository.TokenRespository;

namespace Andoromeda.Kyubey.Dex.Controllers
{
    public class NodeController : BaseController
    {
        [HttpGet("/api/v1/lang/{lang}/node/{account}/balance/")]
        [ProducesResponseType(typeof(ApiResult<Dictionary<string, double>>), 200)]
        [ProducesResponseType(typeof(ApiResult), 404)]
        [ProducesResponseType(typeof(ApiResult), 500)]
        public async Task<IActionResult> GetEOSBalanceAsync(string account, string lang, [FromServices]ILogger logger, [FromServices] KyubeyContext db, [FromServices]TokenRepositoryFactory tokenRepositoryFactory, [FromServices] NodeApiInvoker nodeApiInvoker, CancellationToken cancellationToken)
        {
            var tokenRespository = await tokenRepositoryFactory.CreateAsync(lang);

            var buyTokens = await db.DexBuyOrders.Where(x => x.Account == account).Select(x => x.TokenId).Distinct().ToListAsync(cancellationToken);
            var sellTokens = await db.DexSellOrders.Where(x => x.Account == account).Select(x => x.TokenId).Distinct().ToListAsync(cancellationToken);

            var tokens = buyTokens.Concat(sellTokens).Distinct().ToList();
            var responseData = new Dictionary<string, double>();

            tokens.ForEach(x =>
            {
                var currentTokenBalance = 0.0;
                try
                {
                    var tokenInfo = tokenRespository.GetSingle(x);
                    if (tokenInfo != null)
                    {
                        currentTokenBalance = nodeApiInvoker.GetCurrencyBalanceAsync(account, tokenInfo?.Basic?.Contract?.Transfer, x, cancellationToken).Result;
                    }
                }
                catch (Newtonsoft.Json.JsonSerializationException ex)
                {
                    logger.LogError(ex.ToString());
                }
                finally
                {
                    responseData.Add(x, currentTokenBalance);
                }
            });
            return ApiResult(responseData);
        }

        [HttpGet("/api/v1/lang/{lang}/node/{account}/table/{table}/code/{code}")]
        [ProducesResponseType(typeof(ApiResult<object>), 200)]
        [ProducesResponseType(typeof(ApiResult), 404)]
        [ProducesResponseType(typeof(ApiResult), 500)]
        public async Task<IActionResult> GetEOSTableAsync([FromServices] NodeApiInvoker nodeApiInvoker, GetEOSTableRequest request, CancellationToken cancellationToken)
        {
            var responseData = await nodeApiInvoker.GetTableRowsAsync<object>(request.Code, request.Table, request.Account, request.Skip, request.Take, cancellationToken);
            return ApiResult(responseData);
        }

        [HttpGet("/api/v1/lang/{lang}/node/{account}/action")]
        [ProducesResponseType(typeof(ApiResult<IEnumerable<GetEOSActionsResponse>>), 200)]
        [ProducesResponseType(typeof(ApiResult), 404)]
        [ProducesResponseType(typeof(ApiResult), 500)]
        public async Task<IActionResult> GetEOSActionsAsync([FromServices] NodeApiInvoker nodeApiInvoker, GetEOSActionsRequest request, CancellationToken cancellationToken)
        {
            var apiActions = await nodeApiInvoker.GetActionsAsync(request.Account, request.Skip, request.Take, cancellationToken);

            var responseData = apiActions.actions.Select(x => new GetEOSActionsResponse()
            {
                act = x.action_trace.act,
                time = x.action_trace.block_time
            });

            return ApiResult(responseData);
        }
    }
}
