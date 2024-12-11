using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Http;
using System.Net;
using System.Security.Cryptography;
using Newtonsoft.Json.Linq;
using Nain.Utility;

namespace Nain.BinanceAPI
{
    public delegate void RequestSendingHandler(object sender, HTTPRequestArgs args);
    //public delegate void MarginWSConnected(object sender, EventArgs args);
    //public delegate void MarginWSError(object sender, EventArgs args);

    public class HTTPRequestArgs : EventArgs
    {
        public HttpMethod Method { get; set; }
        public Uri Uri { get; set; }
        public HttpContent Content { get; set; }
        public System.Net.Http.Headers.HttpRequestHeaders Headers { get; set; }
    }

    public class BinanceAPI : IDisposable
    {
        public event RequestSendingHandler RequestSending;
        //public event MarginWSConnected MarginWSConnected;
        //public event MarginWSError MarginWSError;

        protected virtual void OnRequestSending(HttpRequestMessage msg)
        {
            RequestSending?.Invoke(this, new HTTPRequestArgs
            //RequestSending?.BeginInvoke(this, new HTTPRequestArgs
            {
                Content = msg.Content,
                Headers = msg.Headers,
                Method = msg.Method,
                Uri = msg.RequestUri
            });
            //}, null, null);
        }

        //private void OnMarginWSConnected()
        //{
        //    if (MarginWSConnected == null) return;
        //    MarginWSConnected(this, EventArgs.Empty);
        //}

        //private void OnMarginWSError()
        //{
        //    if (MarginWSError == null) return;
        //    MarginWSError(this, EventArgs.Empty);
        //}

        private HttpClient Client = new HttpClient();
        private HMACSHA256 HashObj = new HMACSHA256();
        private Timer RequestsTimer;
        private string BaseEndpoint = "";
        //private DateTime LastRequestTime = new DateTime();
        //private int RequestDelayMilliseconds; //-- DELAY IN MILLISECONDS BETWEEN EVERY REQUEST
        private readonly object APILockObj = new object();

        //private static WebSocket MarginWSCLient;

        /// <param name="requestsTimeoutMillis">Timeout for all requests.</param>
        /// <param name="requestDelayMillis">Delay time between every requests, in milliseconds.</param>
        public BinanceAPI(string baseEndpoint = "https://api.binance.com", string apiKey = "", string secretKey = "", uint requestsTimeoutMillis = 15000,
            uint requestDelayMillis = 0)
        {
            ConfigureProperties(baseEndpoint, apiKey, secretKey, requestsTimeoutMillis, requestDelayMillis);
            //Client.Timeout = TimeSpan.FromSeconds(15);
        }

        /// <inheritdoc cref="BinanceAPI.BinanceAPI"/>
        public void ConfigureProperties(string baseEndpoint = "https://api.binance.com", string apiKey = "", string secretKey = "", uint requestsTimeoutMillis = 15000,
            uint requestDelayMillis = 0)
        {
            Client = new HttpClient();
            BaseEndpoint = baseEndpoint;
            //RequestDelayMilliseconds = requestDelayMilliseconds;
            Client.Timeout = requestsTimeoutMillis == 0 ? TimeSpan.FromMilliseconds(1) : TimeSpan.FromMilliseconds(requestsTimeoutMillis);
            RequestsTimer = new Timer(TimeSpan.FromMilliseconds(requestDelayMillis), false);
            HashObj = new HMACSHA256(Encoding.UTF8.GetBytes(secretKey));
            Client.DefaultRequestHeaders.Remove("X-MBX-APIKEY");
            Client.DefaultRequestHeaders.Add("X-MBX-APIKEY", apiKey);
        }

        private string GenerateSignature(string data)
        {
            //string signature = Convert.ToBase64String(hashObj.ComputeHash(Encoding.UTF32.GetBytes(data)));
            return BitConverter.ToString(HashObj.ComputeHash(Encoding.UTF8.GetBytes(data))).Replace("-", "").ToLower();
            //string signature = System.Text.Encoding.UTF8.GetString(hashObj.ComputeHash(Encoding.UTF8.GetBytes(data)),);
        }

        private void HandleRequestDelay()
        {
            lock (APILockObj)
            {
                //while ((DateTime.Now - LastRequestTime).TotalMilliseconds < RequestDelayMilliseconds) System.Threading.Thread.Sleep(100);
                //LastRequestTime = DateTime.Now;

                RequestsTimer.Wait();
                RequestsTimer.Start();
            }
        }

        private string GetCurrentUnixTimeMillisStr()
        {
            return Convert.ToInt64(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalMilliseconds).ToString();
        }

        //-- SPOT ACCOUNT

        /// <param name="newClientOrderId">A unique id among open orders. Automatically generated if not sent.</param>
        /// <param name="recvWindow">Number of milliseconds after timestamp the request is valid for. The value cannot be greater than 60000.</param>
        /// <param name="newOrderRespType">Set the response JSON. MARKET and LIMIT order types default to FULL, all other orders default to ACK.</param>
        /// <param name="testRequest"></param>
        public JObject SpotLimitOrder(string symbol, OrderSide side, decimal quantity, decimal price, OrderTimeInForce timeInForce, string newClientOrderId = "",
            int recvWindow = 5000, OrderRespType newOrderRespType = OrderRespType.FULL, bool testRequest = false)
        {
            HandleRequestDelay();
            string endpoint = "/api/v3/order" + (testRequest ? "/test" : "");

            string sideStr = side.ToString();
            string quantityStr = quantity.ToString("0.########");
            string priceStr = price.ToString("0.########");
            string timestampStr = GetCurrentUnixTimeMillisStr();
            string timeInForceStr = timeInForce.ToString();
            string recvWindowStr = (recvWindow > 60000 ? 60000 : recvWindow).ToString();
            string newOrderRespTypeStr = newOrderRespType.ToString();

            string data = $"symbol={symbol}&side={sideStr}&type=LIMIT&quantity={quantityStr}&price={priceStr}&timeInForce={timeInForceStr}" +
                $"{(newClientOrderId == "" ? "" : $"&newClientOrderId={newClientOrderId}")}&recvWindow={recvWindowStr}&newOrderRespType={newOrderRespTypeStr}" +
                $"&timestamp={timestampStr}";

            var parameters = new Dictionary<string, string>();
            parameters.Add("symbol", symbol);
            parameters.Add("side", sideStr);
            parameters.Add("type", "LIMIT");
            parameters.Add("quantity", quantityStr);
            parameters.Add("price", priceStr);
            parameters.Add("timeInForce", timeInForceStr);
            if (newClientOrderId != "") parameters.Add("newClientOrderId", newClientOrderId);
            parameters.Add("recvWindow", recvWindowStr);
            parameters.Add("newOrderRespType", newOrderRespTypeStr);
            parameters.Add("timestamp", timestampStr);
            parameters.Add("signature", GenerateSignature(data));

            return SendRequest($"{BaseEndpoint}{endpoint}", HttpMethod.Post, parameters) as JObject;
        }

        /// <inheritdoc cref="SpotLimitOrder"/>
        public JObject SpotMarketOrder(string symbol, OrderSide side, MarketOrderQtyType quantityType, decimal quantity, string newClientOrderId = "",
            int recvWindow = 5000, OrderRespType newOrderRespType = OrderRespType.FULL, bool testRequest = false)
        {
            HandleRequestDelay();
            string endpoint = "/api/v3/order" + (testRequest ? "/test" : "");

            string sideStr = side.ToString();
            string quantityStr = quantity.ToString("0.########");
            string timestampStr = GetCurrentUnixTimeMillisStr();
            string recvWindowStr = (recvWindow > 60000 ? 60000 : recvWindow).ToString();
            string newOrderRespTypeStr = newOrderRespType.ToString();

            string data = $"symbol={symbol}&side={sideStr}&type=MARKET{(quantityType == MarketOrderQtyType.Quote ? "&quoteOrderQty=" : "&quantity=")}{quantityStr}" +
                $"{(newClientOrderId == "" ? "" : $"&newClientOrderId={newClientOrderId}")}&recvWindow={recvWindowStr}&newOrderRespType={newOrderRespTypeStr}" +
                $"&timestamp={timestampStr}";

            var parameters = new Dictionary<string, string>();
            parameters.Add("symbol", symbol);
            parameters.Add("side", sideStr);
            parameters.Add("type", "MARKET");
            
            if (quantityType == MarketOrderQtyType.Quote)
                parameters.Add("quoteOrderQty", quantityStr);
            else 
                parameters.Add("quantity", quantityStr);

            if (newClientOrderId != "") parameters.Add("newClientOrderId", newClientOrderId);
            parameters.Add("recvWindow", recvWindowStr);
            parameters.Add("newOrderRespType", newOrderRespTypeStr);
            parameters.Add("timestamp", timestampStr);
            parameters.Add("signature", GenerateSignature(data));

            return SendRequest($"{BaseEndpoint}{endpoint}", HttpMethod.Post, parameters) as JObject;
        }

        public JObject CancelSpotOrder(string symbol, long orderId, string newClientOrderId = "", int recvWindow = 5000)
        {
            HandleRequestDelay();

            string timestampStr = GetCurrentUnixTimeMillisStr();
            string recvWindowStr = (recvWindow > 60000 ? 60000 : recvWindow).ToString();

            string data = $"symbol={symbol}&orderId={orderId}{(newClientOrderId == "" ? "" : $"&newClientOrderId={newClientOrderId}")}" +
                $"&timestamp={timestampStr}&recvWindow={recvWindowStr}";

            return SendRequest($"{BaseEndpoint}/api/v3/order?{data}&signature={GenerateSignature(data)}", HttpMethod.Delete) as JObject;
        }

        public JObject CancelSpotOrder(string symbol, string origClientOrderId, string newClientOrderId = "", int recvWindow = 5000)
        {
            HandleRequestDelay();

            string timestampStr = GetCurrentUnixTimeMillisStr();
            string recvWindowStr = (recvWindow > 60000 ? 60000 : recvWindow).ToString();

            string data = $"symbol={symbol}&origClientOrderId={origClientOrderId}{(newClientOrderId == "" ? "" : $"&newClientOrderId={newClientOrderId}")}" +
                $"&timestamp={timestampStr}&recvWindow={recvWindowStr}";

            return SendRequest($"{BaseEndpoint}/api/v3/order?{data}&signature={GenerateSignature(data)}", HttpMethod.Delete) as JObject;
        }

        public JObject QuerySpotOrder(string symbol, long orderId = 0, int recvWindow = 5000)
        {
            HandleRequestDelay();

            string timestampStr = GetCurrentUnixTimeMillisStr();
            string recvWindowStr = (recvWindow > 60000 ? 60000 : recvWindow).ToString();

            string data = $"symbol={symbol}&orderId={orderId}&timestamp={timestampStr}&recvWindow={recvWindowStr}";

            return SendRequest($"{BaseEndpoint}/api/v3/order?{data}&signature={GenerateSignature(data)}", HttpMethod.Get) as JObject;
        }

        public JObject QuerySpotOrder(string symbol, string origClientOrderId, int recvWindow = 5000)
        {
            HandleRequestDelay();

            string timestampStr = GetCurrentUnixTimeMillisStr();
            string recvWindowStr = (recvWindow > 60000 ? 60000 : recvWindow).ToString();

            string data = $"symbol={symbol}&origClientOrderId={origClientOrderId}&timestamp={timestampStr}&recvWindow={recvWindowStr}";

            return SendRequest($"{BaseEndpoint}/api/v3/order?{data}&signature={GenerateSignature(data)}", HttpMethod.Get) as JObject ;
        }

        public JArray QuerySpotOpenOrders(string symbol = "", int recvWindow = 5000)
        {
            HandleRequestDelay();
            string timestampStr = GetCurrentUnixTimeMillisStr();
            string recvWindowStr = (recvWindow > 60000 ? 60000 : recvWindow).ToString();

            string data = (symbol == "" ? "" : $"symbol={symbol}&") + $"timestamp={timestampStr}&recvWindow={recvWindowStr}";

            return SendRequest($"{BaseEndpoint}/api/v3/openOrders?{data}&signature={GenerateSignature(data)}", HttpMethod.Get) as JArray;
        }

        //-- If orderId is set, it will get orders >= that orderId. Otherwise most recent orders are returned.
        public JArray QueryAllSpotOrders(string symbol, long orderId = 0, long startTimeUnixMillis = 0, long endTimeUnixMillis = 0,
            int limit = 500, int recvWindow = 5000)
        {
            HandleRequestDelay();
            string endpoint = "/api/v3/allOrders";

            string orderIdStr = orderId.ToString();
            string startTimeStr = startTimeUnixMillis.ToString();
            string endTimeStr = endTimeUnixMillis.ToString();
            string limitStr = (limit > 1000 ? 1000 : limit).ToString();
            string timestampStr = GetCurrentUnixTimeMillisStr();
            string recvWindowStr = (recvWindow > 60000 ? 60000 : recvWindow).ToString();

            string data = $"symbol={symbol}" + (orderId == 0 ? "" : $"&orderId={orderIdStr}") +
                (startTimeUnixMillis == 0 ? "" : $"&startTime={startTimeStr}") +
                (endTimeUnixMillis == 0 ? "" : $"&endTime={endTimeStr}") +
                (limit == 0 ? "" : $"&limit={limitStr}") +
                $"&timestamp={timestampStr}&recvWindow={recvWindowStr}";

            return SendRequest($"{BaseEndpoint}{endpoint}?{data}&signature={GenerateSignature(data)}", HttpMethod.Get) as JArray;
        }

        /// <summary>
        /// Get trades for a specific account and symbol. Weight(IP): 10.
        /// </summary>
        /// <param name="orderId">This can only be used in combination with symbol.</param>
        /// <param name="fromId">TradeId to fetch from. Default gets most recent trades.</param>
        /// <param name="recvWindow">The value cannot be greater than 60000.</param>
        /// <returns></returns>
        public JArray QuerySpotTrades(string symbol, long orderId = 0, long startTimeUnixMillis = 0, long endTimeUnixMillis = 0,
            int fromId = 0, int limit = 500, int recvWindow = 5000)
        {
            string endpoint = "/api/v3/myTrades";

            string orderIdStr = orderId.ToString();
            string startTimeStr = startTimeUnixMillis.ToString();
            string endTimeStr = endTimeUnixMillis.ToString();
            string limitStr = (limit > 1000 ? 1000 : limit).ToString();
            string recvWindowStr = (recvWindow > 60000 ? 60000 : recvWindow).ToString();
            string timestampStr = GetCurrentUnixTimeMillisStr();

            string data = $"symbol={symbol}" + (orderId == 0 ? "" : $"&orderId={orderIdStr}") +
                (startTimeUnixMillis == 0 ? "" : $"&startTime={startTimeStr}") +
                (endTimeUnixMillis == 0 ? "" : $"&endTime={endTimeStr}") +
                (fromId == 0 ? "" : $"&fromId={fromId}") +
                (limit == 0 ? "" : $"&limit={limitStr}") +
                $"&timestamp={timestampStr}&recvWindow={recvWindowStr}";

            return SendRequest($"{BaseEndpoint}{endpoint}?{data}&signature={GenerateSignature(data)}", HttpMethod.Get) as JArray;
        }

        //-- MARKET DATA
        public void Ping() => SendRequest($"{BaseEndpoint}/api/v3/ping", HttpMethod.Get);

        /// <summary>
        /// Current exchange trading rules and symbol information.
        /// </summary>
        public JObject GetExchangeInfo()
        {
            HandleRequestDelay();
            return SendRequest($"{BaseEndpoint}/api/v3/exchangeInfo", HttpMethod.Get) as JObject;
        }

        /// <inheritdoc cref="GetExchangeInfo()"/>
        public JObject GetExchangeInfo(string symbol)
        {
            HandleRequestDelay();
            return SendRequest($"{BaseEndpoint}/api/v3/exchangeInfo?symbol={symbol}", HttpMethod.Get) as JObject;
        }

        /// <inheritdoc cref="GetExchangeInfo()"/>
        public JObject GetExchangeInfo(string[] symbols)
        {
            HandleRequestDelay();
            string symbolsStr = "[\"" + string.Join("\",\"", symbols) + "\"]";
            return SendRequest($"{BaseEndpoint}/api/v3/exchangeInfo?symbols={symbolsStr}", HttpMethod.Get) as JObject;
        }

        /// <summary>
        /// Latest price for a symbol or symbols.
        /// </summary>
        public JObject PriceTicker(string symbol = "")
        {
            HandleRequestDelay();
            string data = symbol == "" ? "" : $"symbol={symbol}";
            return SendRequest($"{BaseEndpoint}/api/v3/ticker/price?{data}", HttpMethod.Get) as JObject;
        }

        public JArray CandlestickData(string symbol, ChartInterval interval, long startTimeUnixMillis = 0, long endTimeUnixMillis = 0, int limit = 500)
        {
            HandleRequestDelay();
            string data = $"symbol={symbol}&interval={EnumToString(interval)}" + (startTimeUnixMillis > 0 ? $"&startTime={startTimeUnixMillis}" : "") +
                (endTimeUnixMillis > 0 ? $"&endTime={endTimeUnixMillis}" : "") + (limit != 500 ? $"&limit={limit}" : "");
            return SendRequest($"{BaseEndpoint}/api/v3/klines?{data}", HttpMethod.Get) as JArray;
        }

        public JObject AveragePrice(string symbol)
        {
            HandleRequestDelay();
            return SendRequest($"{BaseEndpoint}/api/v3/avgPrice?symbol={symbol}", HttpMethod.Get) as JObject;
        }

        //-- USER DATA STREAMS
        public JObject CreateSpotListenKey()
        {
            HandleRequestDelay();
            return SendRequest($"{BaseEndpoint}/api/v3/userDataStream", HttpMethod.Post) as JObject;
        }

        public void CloseSpotListenKey(string listenKey)
        {
            HandleRequestDelay();
            SendRequest($"{BaseEndpoint}/api/v3/userDataStream?listenKey={listenKey}", HttpMethod.Delete);
        }

        public void RenewSpotListenKey(string listenKey)
        {
            HandleRequestDelay();
            SendRequest($"{BaseEndpoint}/api/v3/userDataStream?listenKey={listenKey}", HttpMethod.Put);
        }

        public JObject CreateCrossMarginListenKey()
        {
            HandleRequestDelay();
            return SendRequest($"{BaseEndpoint}/sapi/v1/userDataStream", HttpMethod.Post) as JObject;
        }

        public void CloseCrossMarginListenKey(string listenKey)
        {
            HandleRequestDelay();
            SendRequest($"{BaseEndpoint}/sapi/v1/userDataStream?listenKey={listenKey}", HttpMethod.Delete);
        }

        public void RenewCrossMarginListenKey(string listenKey)
        {
            HandleRequestDelay();
            SendRequest($"{BaseEndpoint}/sapi/v1/userDataStream?listenKey={listenKey}", HttpMethod.Put);
        }

        public JObject CreateIsolatedMarginListenKey(string symbol)
        {
            HandleRequestDelay();
            return SendRequest($"{BaseEndpoint}/sapi/v1/userDataStream/isolated?symbol={symbol}", HttpMethod.Post) as JObject;
        }

        public void CloseIsolatedMarginListenKey(string symbol, string listenKey)
        {
            HandleRequestDelay();
            SendRequest($"{BaseEndpoint}/sapi/v1/userDataStream/isolated?symbol={symbol}&listenKey={listenKey}", HttpMethod.Delete);
        }

        public void RenewIsolatedMarginListenKey(string symbol, string listenKey)
        {
            HandleRequestDelay();
            SendRequest($"{BaseEndpoint}/sapi/v1/userDataStream/isolated?symbol={symbol}&listenKey={listenKey}", HttpMethod.Put);
        }

        //-- MARGIN ACCOUNT

        /// <inheritdoc cref="SpotLimitOrder"/>
        public JObject MarginLimitOrder(string symbol, OrderSide side, decimal quantity, decimal price, OrderTimeInForce timeInForce, bool isIsolated = false,
            OrderSideEffect sideEffectType = OrderSideEffect.NO_SIDE_EFFECT, string newClientOrderId = "", int recvWindow = 5000, 
            OrderRespType newOrderRespType = OrderRespType.FULL)
        {
            HandleRequestDelay();
            string sideStr = side.ToString();
            string quantityStr = quantity.ToString("0.########");
            string priceStr = price.ToString("0.########");
            string timestampStr = GetCurrentUnixTimeMillisStr();
            string timeInForceStr = timeInForce.ToString();
            string recvWindowStr = (recvWindow > 60000 ? 60000 : recvWindow).ToString();
            string newOrderRespTypeStr = newOrderRespType.ToString();

            string data = $"symbol={symbol}&side={sideStr}&type=LIMIT&quantity={quantityStr}&price={priceStr}" +
                $"&timeInForce={timeInForceStr}{(isIsolated ? $"&isIsolated=TRUE" : "")}" +
                $"{(sideEffectType == OrderSideEffect.NO_SIDE_EFFECT ? "" : $"&sideEffectType={sideEffectType}")}" +
                $"{(newClientOrderId == "" ? "" : $"&newClientOrderId={newClientOrderId}")}" +
                $"&recvWindow={recvWindowStr}&newOrderRespType={newOrderRespTypeStr}&timestamp={timestampStr}";

            var parameters = new Dictionary<string, string>();
            parameters.Add("symbol", symbol);
            parameters.Add("side", sideStr);
            parameters.Add("type", "LIMIT");
            parameters.Add("quantity", quantityStr);
            parameters.Add("price", priceStr);
            parameters.Add("timeInForce", timeInForceStr);
            if (isIsolated) parameters.Add("isIsolated", "TRUE");
            if (sideEffectType != OrderSideEffect.NO_SIDE_EFFECT) parameters.Add("sideEffectType", sideEffectType.ToString());
            if (newClientOrderId != "") parameters.Add("newClientOrderId", newClientOrderId);
            parameters.Add("recvWindow", recvWindowStr);
            parameters.Add("newOrderRespType", newOrderRespTypeStr);
            parameters.Add("timestamp", timestampStr);
            parameters.Add("signature", GenerateSignature(data));

            return SendRequest($"{BaseEndpoint}/sapi/v1/margin/order", HttpMethod.Post, parameters) as JObject;
        }

        /// <inheritdoc cref="SpotLimitOrder"/>
        public JObject MarginMarketOrder(string symbol, OrderSide side, MarketOrderQtyType quantityType, decimal quantity, bool isIsolated = false,
            OrderSideEffect sideEffectType = OrderSideEffect.NO_SIDE_EFFECT, string newClientOrderId = "", int recvWindow = 5000,
            OrderRespType newOrderRespType = OrderRespType.FULL)
        {
            HandleRequestDelay();
            string sideStr = side.ToString();
            string quantityStr = quantity.ToString("0.########");
            string timestampStr = GetCurrentUnixTimeMillisStr();
            string recvWindowStr = (recvWindow > 60000 ? 60000 : recvWindow).ToString();
            string newOrderRespTypeStr = newOrderRespType.ToString();

            string data = $"symbol={symbol}&side={sideStr}&type=MARKET{(quantityType == MarketOrderQtyType.Quote ? "&quoteOrderQty=" : "&quantity=")}{quantityStr}" +
                $"{(isIsolated ? $"&isIsolated=TRUE" : "")}{(sideEffectType == OrderSideEffect.NO_SIDE_EFFECT ? "" : $"&sideEffectType={sideEffectType}")}" +
                $"{(newClientOrderId == "" ? "" : $"&newClientOrderId={newClientOrderId}")}&recvWindow={recvWindowStr}&newOrderRespType={newOrderRespTypeStr}" +
                $"&timestamp={timestampStr}";

            var parameters = new Dictionary<string, string>();
            parameters.Add("symbol", symbol);
            parameters.Add("side", sideStr);
            parameters.Add("type", "MARKET");
            if (quantityType == MarketOrderQtyType.Quote) parameters.Add("quoteOrderQty", quantityStr);
            else parameters.Add("quantity", quantityStr);
            if (isIsolated) parameters.Add("isIsolated", "TRUE");
            if (sideEffectType != OrderSideEffect.NO_SIDE_EFFECT) parameters.Add("sideEffectType", sideEffectType.ToString());
            if (newClientOrderId != "") parameters.Add("newClientOrderId", newClientOrderId);
            parameters.Add("recvWindow", recvWindowStr);
            parameters.Add("newOrderRespType", newOrderRespTypeStr);
            parameters.Add("timestamp", timestampStr);
            parameters.Add("signature", GenerateSignature(data));

            return SendRequest($"{BaseEndpoint}/sapi/v1/margin/order", HttpMethod.Post, parameters) as JObject;
        }

        public JObject CancelMarginOrder(string symbol, long orderId, bool isIsolated = false, string newClientOrderId = "", int recvWindow = 5000)
        {
            HandleRequestDelay();

            string timestampStr = GetCurrentUnixTimeMillisStr();
            string recvWindowStr = (recvWindow > 60000 ? 60000 : recvWindow).ToString();

            string data = $"symbol={symbol}&orderId={orderId}{(isIsolated ? $"&isIsolated=TRUE" : "")}" +
                $"{(newClientOrderId == "" ? "" : $"&newClientOrderId={newClientOrderId}")}&timestamp={timestampStr}&recvWindow={recvWindowStr}";

            return SendRequest($"{BaseEndpoint}/sapi/v1/margin/order?{data}&signature={GenerateSignature(data)}", HttpMethod.Delete) as JObject;
        }

        public JObject CancelMarginOrder(string symbol, string origClientOrderId, bool isIsolated = false, string newClientOrderId = "", int recvWindow = 5000)
        {
            HandleRequestDelay();

            string timestampStr = GetCurrentUnixTimeMillisStr();
            string recvWindowStr = (recvWindow > 60000 ? 60000 : recvWindow).ToString();

            string data = $"symbol={symbol}&origClientOrderId={origClientOrderId}{(isIsolated ? $"&isIsolated=TRUE" : "")}" +
                $"{(newClientOrderId == "" ? "" : $"&newClientOrderId={newClientOrderId}")}&timestamp={timestampStr}&recvWindow={recvWindowStr}";

            return SendRequest($"{BaseEndpoint}/sapi/v1/margin/order?{data}&signature={GenerateSignature(data)}", HttpMethod.Delete) as JObject;
        }

        public JObject QueryMarginOrder(string symbol, long orderId, bool isIsolated = false, int recvWindow = 5000)
        {
            HandleRequestDelay();

            string timestampStr = GetCurrentUnixTimeMillisStr();
            string recvWindowStr = (recvWindow > 60000 ? 60000 : recvWindow).ToString();

            string data = $"symbol={symbol}&orderId={orderId}{(isIsolated ? $"&isIsolated=TRUE" : "")}&timestamp={timestampStr}&recvWindow={recvWindowStr}";

            return SendRequest($"{BaseEndpoint}/sapi/v1/margin/order?{data}&signature={GenerateSignature(data)}", HttpMethod.Get) as JObject;
        }

        public JObject QueryMarginOrder(string symbol, string origClientOrderId, bool isIsolated = false, int recvWindow = 5000)
        {
            HandleRequestDelay();

            string timestampStr = GetCurrentUnixTimeMillisStr();
            string recvWindowStr = (recvWindow > 60000 ? 60000 : recvWindow).ToString();

            string data = $"symbol={symbol}&origClientOrderId={origClientOrderId}{(isIsolated ? $"&isIsolated=TRUE" : "")}&timestamp={timestampStr}" +
                $"&recvWindow={recvWindowStr}";

            return SendRequest($"{BaseEndpoint}/sapi/v1/margin/order?{data}&signature={GenerateSignature(data)}", HttpMethod.Get) as JObject;
        }

        public JArray QueryMarginOpenOrders(string symbol = "", bool isIsolated = false, int recvWindow = 5000)
        {
            HandleRequestDelay();
            string timestampStr = GetCurrentUnixTimeMillisStr();
            string recvWindowStr = (recvWindow > 60000 ? 60000 : recvWindow).ToString();

            string data = $"{(symbol == "" ? "" : $"symbol={symbol}&")}{(isIsolated ? $"isIsolated=TRUE" : "")}&timestamp={timestampStr}&recvWindow={recvWindowStr}";

            return SendRequest($"{BaseEndpoint}/sapi/v1/margin/openOrders?{data}&signature={GenerateSignature(data)}", HttpMethod.Get) as JArray;
        }

        //-- If orderId is set, it will get orders >= that orderId. Otherwise most recent orders are returned.
        public JArray QueryAllMarginOrders(string symbol, bool isIsolated = false, long orderId = 0, long startTimeUnixMillis = 0, long endTimeUnixMillis = 0,
            int limit = 500, int recvWindow = 5000)
        {
            HandleRequestDelay();

            string orderIdStr = orderId.ToString();
            string startTimeStr = startTimeUnixMillis.ToString();
            string endTimeStr = endTimeUnixMillis.ToString();
            string limitStr = (limit > 1000 ? 1000 : limit).ToString();
            string timestampStr = GetCurrentUnixTimeMillisStr();
            string recvWindowStr = (recvWindow > 60000 ? 60000 : recvWindow).ToString();

            string data = $"symbol={symbol}{(orderId == 0 ? "" : $"&orderId={orderIdStr}")}{(isIsolated ? $"&isIsolated=TRUE" : "")}" +
                (startTimeUnixMillis == 0 ? "" : $"&startTime={startTimeStr}") +
                (endTimeUnixMillis == 0 ? "" : $"&endTime={endTimeStr}") +
                (limit == 0 ? "" : $"&limit={limitStr}") +
                $"&timestamp={timestampStr}&recvWindow={recvWindowStr}";

            return SendRequest($"{BaseEndpoint}/sapi/v1/margin/allOrders?{data}&signature={GenerateSignature(data)}", HttpMethod.Get) as JArray;
        }

        public JObject MarginPriceIndex(string symbol)
        {
            HandleRequestDelay();
            return SendRequest($"{BaseEndpoint}/sapi/v1/margin/priceIndex?symbol={symbol}", HttpMethod.Get) as JObject;
        }

        //-- Max 5 symbols
        public JObject GetIsolatedMarginAccountInfo(string[] symbols = null, int recvWindow = 5000)
        {
            HandleRequestDelay();
            string recvWindowStr = (recvWindow > 60000 ? 60000 : recvWindow).ToString();
            //string symbolsStr = "[\"" + string.Join("\",\"", symbols) + "\"]";

            string data = $"{(symbols == null ? "" : $"symbols=\"{string.Join(",", symbols)}\"&")}timestamp={GetCurrentUnixTimeMillisStr()}&recvWindow={recvWindowStr}";
            return SendRequest($"{BaseEndpoint}/sapi/v1/margin/isolated/account?{data}&signature={GenerateSignature(data)}", HttpMethod.Get) as JObject;
        }

        public JArray GetAllCrossMarginPairs()
        {
            HandleRequestDelay();
            return SendRequest($"{BaseEndpoint}/sapi/v1/margin/allPairs", HttpMethod.Get) as JArray;
        }

        public JObject GetCrossMarginAccountDetails(int recvWindow = 5000)
        {
            HandleRequestDelay();
            string recvWindowStr = (recvWindow > 60000 ? 60000 : recvWindow).ToString();
            string data = $"timestamp={GetCurrentUnixTimeMillisStr()}&recvWindow={recvWindowStr}";
            return SendRequest($"{BaseEndpoint}/sapi/v1/margin/account?{data}&signature={GenerateSignature(data)}", HttpMethod.Get) as JObject;
        }

        /// <summary>
        /// Get trades for a specific account and symbol. Weight(IP): 10.
        /// </summary>
        /// <param name="orderId">This can only be used in combination with symbol.</param>
        /// <param name="fromId">TradeId to fetch from. Default gets most recent trades.</param>
        /// <param name="recvWindow">The value cannot be greater than 60000.</param>
        /// <returns></returns>
        public JArray QueryMarginTrades(string symbol, bool isIsolated = false, long orderId = 0, long startTimeUnixMillis = 0, long endTimeUnixMillis = 0,
            int fromId = 0, int limit = 500, int recvWindow = 5000)
        {
            string endpoint = "/sapi/v1/margin/myTrades";

            string orderIdStr = orderId.ToString();
            string startTimeStr = startTimeUnixMillis.ToString();
            string endTimeStr = endTimeUnixMillis.ToString();
            string limitStr = (limit > 1000 ? 1000 : limit).ToString();
            string recvWindowStr = (recvWindow > 60000 ? 60000 : recvWindow).ToString();
            string timestampStr = GetCurrentUnixTimeMillisStr();

            string data = $"symbol={symbol}" + (!isIsolated ? "" : $"&isIsolated={isIsolated}") + 
                (orderId == 0 ? "" : $"&orderId={orderIdStr}") +
                (startTimeUnixMillis == 0 ? "" : $"&startTime={startTimeStr}") +
                (endTimeUnixMillis == 0 ? "" : $"&endTime={endTimeStr}") +
                (fromId == 0 ? "" : $"&fromId={fromId}") +
                (limit == 0 ? "" : $"&limit={limitStr}") +
                $"&timestamp={timestampStr}&recvWindow={recvWindowStr}";

            return SendRequest($"{BaseEndpoint}{endpoint}?{data}&signature={GenerateSignature(data)}", HttpMethod.Get) as JArray;
        }

        //-- WALLET
        public JObject GetTradeFee(string symbol = "", int recvWindow = 5000)
        {
            HandleRequestDelay();
            string timestampStr = GetCurrentUnixTimeMillisStr();
            string recvWindowStr = (recvWindow > 60000 ? 60000 : recvWindow).ToString();
            string data = (symbol == "" ? "" : $"symbol={symbol}&") + $"timestamp={timestampStr}&recvWindow={recvWindowStr}";
            return SendRequest($"{BaseEndpoint}/sapi/v1/asset/tradeFee?{data}&signature={GenerateSignature(data)}", HttpMethod.Get) as JObject;
        }

        public JArray GetSpotAccountCoins(int recvWindow = 5000)
        {
            HandleRequestDelay();
            string endpoint = "/sapi/v1/capital/config/getall";
            string timestampStr = GetCurrentUnixTimeMillisStr();
            string recvWindowStr = (recvWindow > 60000 ? 60000 : recvWindow).ToString();
            string data = $"timestamp={timestampStr}&recvWindow={recvWindowStr}";
            return SendRequest($"{BaseEndpoint}{endpoint}?{data}&signature={GenerateSignature(data)}", HttpMethod.Get) as JArray;
        }

        //-- REQUEST METHOD

        //private JObject SendRequest(string url, HttpMethod httpMethod, Dictionary<string, string> content = null)
        //{
        //    HttpRequestMessage msg = new HttpRequestMessage(httpMethod, url);
        //    if (content != null) msg.Content = new FormUrlEncodedContent(content);
        //    OnRequestSending(msg);
        //    return SendRequestSync(msg);
        //}

        //private JObject SendRequest(string url, HttpMethod httpMethod, Dictionary<string, string> content = null /*HttpRequestMessage msg*/)
        private JToken SendRequest(string url, HttpMethod httpMethod, Dictionary<string, string> content = null /*HttpRequestMessage msg*/)
        {
            HttpRequestMessage msg = new HttpRequestMessage(httpMethod, url);
            if (content != null) msg.Content = new FormUrlEncodedContent(content);
            OnRequestSending(msg);

            HttpResponseMessage response;
            string result;

            try
            {
                response = Client.SendAsync(msg).Result;
                result = response.Content.ReadAsStringAsync().Result;
                //if (response.StatusCode == HttpStatusCode.NotFound) response.EnsureSuccessStatusCode();
            }
            catch (Exception e)
            {
                //if (response != null)
                //{
                //    if (response.StatusCode == HttpStatusCode.NotFound)
                //        throw new ResponseException("404 not found was returned from the request.", response.StatusCode, "", null);
                //}

                if (e.GetType() == typeof(AggregateException))
                {
                    while (e.InnerException != null)
                        e = e.InnerException;
                }

                throw e;
            }

            if (response.StatusCode == HttpStatusCode.NotFound)
                throw new ResponseException("404 not found was returned from the request.", response.StatusCode, result, null);

            //JObject resultJson;
            JToken resultJson;
            //bool responseIsJson = true;

            try
            {
                resultJson = JToken.Parse(result);
                //resultJson = Newtonsoft.Json.JsonConvert.DeserializeObject<j>(result);
                //resultJson = JObject.Parse(result);
                //var dd = Newtonsoft.Json.Linq.JObject.Parse(result);
                //var r = dd["sdff"];
            }
            catch (Exception e)
            {
                //if (response.StatusCode == HttpStatusCode.OK)
                //throw; //new Exception("Error while deserializing content.");
                throw new ResponseException("Failed to deserialize response content.", response.StatusCode, result, e);

                //responseIsJson = false;
            }

            if (!response.IsSuccessStatusCode) //StatusCode != HttpStatusCode.OK)
            {
                //if (responseIsJson)
                HandleBinanceErrorResponse(response, resultJson);
                //else
                //    throw new Exception($"{(int)response.StatusCode} {response.StatusCode}");

                //throw new System.Net.Sockets.SocketError.
            }

            return resultJson;
        }

        /// <summary>
        /// Attempts to deserialize the Binance return code into a BinanceAPIException.
        /// </summary>
        private void HandleBinanceErrorResponse(HttpResponseMessage response, JToken resultJson)
        {
            APIErrorCode errorCode;
            //int errorCode;
            string message;

            try
            {
                errorCode = (APIErrorCode)int.Parse(resultJson["code"].ToString());
                //errorCode = int.Parse(resultJson.code.ToString());
                message = resultJson["msg"].ToString();
            }
            catch (Exception e)
            {
                //throw new Exception($"{(int)response.StatusCode} {response.StatusCode + Environment.NewLine + resultJson}");
                throw new ResponseException("Failed to deserialize API error response.", response.StatusCode, response.Content.ToString(), e);
            }

            //-- THROWS A BINANCEAPIAEXCEPTION IF THE RETURNED JSON CONTENT IS AN ERROR CODE FROM THE API
            throw new BinanceAPIException(message, errorCode, response.StatusCode);
        }

        public string EnumToString(Enum e)
        {
            if (e is ChartInterval interval)
            {
                switch (interval)
                {
                    case ChartInterval.ONE_MIN:
                        return "1m";
                    case ChartInterval.THREE_MIN:
                        return "3m";
                    case ChartInterval.FIVE_MIN:
                        return "5m";
                    case ChartInterval.FIFTEEN_MIN:
                        return "15m";
                    case ChartInterval.THIRTY_MIN:
                        return "30m";
                    case ChartInterval.ONE_HOUR:
                        return "1h";
                    case ChartInterval.TWO_HOUR:
                        return "2h";
                    case ChartInterval.FOUR_HOUR:
                        return "4h";
                    case ChartInterval.SIX_HOUR:
                        return "6h";
                    case ChartInterval.EIGHT_HOUR:
                        return "8h";
                    case ChartInterval.TWELVE_HOUR:
                        return "12h";
                    case ChartInterval.ONE_DAY:
                        return "1d";
                    case ChartInterval.THREE_DAY:
                        return "3d";
                    case ChartInterval.ONE_WEEK:
                        return "1w";
                    case ChartInterval.ONE_MONTH:
                        return "1M";
                }
            }

            return e.ToString();
        }

        public void Dispose()
        {
            ((IDisposable)Client).Dispose();
        }
    }

    /// <summary>
    /// Occurs when a request reached the exchange and an error code is returned, representing specific API errors.
    /// </summary>
    public class BinanceAPIException : Exception
    {
        public APIErrorCode ErrorCode { get; }
        //public int ErrorCode { get; }
        public HttpStatusCode StatusCode { get; }

        public BinanceAPIException(string message, APIErrorCode errorCode /*int errorCode*/, HttpStatusCode statusCode)
            : base(message)
        {
            ErrorCode = errorCode;
            StatusCode = statusCode;
        }
    }

    /// <summary>
    /// Occurs when a request is sent and a response is retrieved with an error, but doesn't represent an error code 
    /// from the exchange (unlike BinanceAPIException). Could also mean that it's message couldn't be deserialized. 
    /// Used to catch 404 HTTP errors, for example.
    /// </summary>
    public class ResponseException : Exception
    {
        public HttpStatusCode StatusCode { get; }
        public string ResponseText { get; }

        public ResponseException(string message, HttpStatusCode statusCode, string responseText, Exception innerException)
            : base(message, innerException)
        {
            StatusCode = statusCode;
            ResponseText = responseText;
        }
    }
}