using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Http;
using System.Net;
using System.Security.Cryptography;
using Newtonsoft.Json;

namespace BinanceWebAPI
{
    public class BinanceAPI
    {
        private readonly HttpClient Client = new HttpClient();
        private HMACSHA256 HashObj = new HMACSHA256();
        private string BaseEndpoint = "";
        private DateTime LastRequestTime = new DateTime();
        private int RequestDelayMilliseconds = 1000; //-- DELAY IN MILLISECONDS BETWEEN EVERY REQUEST
        private readonly object APILockObj = new object();

        public BinanceAPI(string baseEndpoint, string apiKey, string secretKey, int requestDelayMilliseconds)
        {
            ConfigureProperties(baseEndpoint, apiKey, secretKey, requestDelayMilliseconds);
            Client.Timeout = TimeSpan.FromSeconds(15);
        }

        public void ConfigureProperties(string baseEndpoint, string akey, string skey, int requestDelayMilliseconds)
        {
            BaseEndpoint = baseEndpoint;
            RequestDelayMilliseconds = requestDelayMilliseconds;
            HashObj = new HMACSHA256(Encoding.UTF8.GetBytes(skey));
            Client.DefaultRequestHeaders.Remove("X-MBX-APIKEY");
            Client.DefaultRequestHeaders.Add("X-MBX-APIKEY", akey);
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
                while ((DateTime.Now - LastRequestTime).TotalMilliseconds < RequestDelayMilliseconds) System.Threading.Thread.Sleep(100);
                LastRequestTime = DateTime.Now;
            }
        }

        private string GetCurrentUnixTimeMillisecondsStr()
        {
            return Convert.ToInt64(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalMilliseconds).ToString();
        }

        public object Ping()
        {
            string endpoint = "/api/v3/ping";
            return GetRequest(BaseEndpoint + endpoint);
        }

        public object CreateLimitOrder(string symbol, OrderSide side, decimal quantity, decimal price,
            OrderTimeInForce timeInForce, int recvWindow = 5000, string newClientOrderId = "",
            OrderRespType newOrderRespType = OrderRespType.FULL, bool testRequest = false)
        {
            HandleRequestDelay();
            string endpoint = "/api/v3/order" + (testRequest ? "/test" : "");

            string symbolStr = symbol;
            string sideStr = Enum.GetName(typeof(OrderSide), side);
            string typeStr = OrderType.LIMIT.ToString();
            string quantityStr = quantity.ToString("0.########");
            string priceStr = price.ToString("0.########");
            string timestampStr = GetCurrentUnixTimeMillisecondsStr();
            string timeInForceStr = Enum.GetName(typeof(OrderTimeInForce), timeInForce);
            string recvWindowStr = (recvWindow > 60000 ? 60000 : recvWindow).ToString();
            string newClientOrderIdStr = newClientOrderId;
            string newOrderRespTypeStr = Enum.GetName(typeof(OrderRespType), newOrderRespType);

            string data = "symbol=" + symbolStr + "&side=" + sideStr + "&type=" + typeStr + "&quantity=" + quantityStr + "&price=" +
                priceStr + "&timestamp=" + timestampStr + "&timeInForce=" + timeInForceStr + "&recvWindow=" + recvWindowStr +
                (newClientOrderId == "" ? "" : "&newClientOrderId=" + newClientOrderIdStr) + "&newOrderRespType=" + newOrderRespTypeStr;

            var parameters = new Dictionary<string, string>();
            parameters.Add("symbol", symbolStr);
            parameters.Add("side", sideStr);
            parameters.Add("type", typeStr);
            parameters.Add("quantity", quantityStr);
            parameters.Add("price", priceStr);
            parameters.Add("timestamp", timestampStr);
            parameters.Add("timeInForce", timeInForceStr);
            parameters.Add("recvWindow", recvWindowStr);
            if (newClientOrderId != "") parameters.Add("newClientOrderId", newClientOrderIdStr);
            parameters.Add("newOrderRespType", newOrderRespTypeStr);
            parameters.Add("signature", GenerateSignature(data));

            return PostRequest(BaseEndpoint + endpoint, parameters);
        }

        public object CreateMarketOrder(string symbol, OrderSide side, decimal quantity = 0,
            decimal quoteOrderQty = 0, int recvWindow = 5000, OrderRespType newOrderRespType = OrderRespType.FULL,
            bool testRequest = false)
        {
            HandleRequestDelay();
            string endpoint = "/api/v3/order" + (testRequest ? "/test" : "");

            string symbolStr = symbol;
            string sideStr = Enum.GetName(typeof(OrderSide), side);
            string typeStr = Enum.GetName(typeof(OrderType), OrderType.MARKET);
            string quantityStr = quantity.ToString("0.########");
            string quoteOrderQtyStr = quoteOrderQty.ToString("0.########");
            string timestampStr = GetCurrentUnixTimeMillisecondsStr();
            string recvWindowStr = (recvWindow > 60000 ? 60000 : recvWindow).ToString();
            string newOrderRespTypeStr = Enum.GetName(typeof(OrderRespType), newOrderRespType);

            string data = "symbol=" + symbolStr + "&side=" + sideStr + "&type=" + typeStr +
                (quantity == 0 ? "&quoteOrderQty=" + quoteOrderQtyStr : "&quantity=" + quantityStr) +
                "&timestamp=" + timestampStr + "&recvWindow=" + recvWindowStr +
                "&newOrderRespType=" + newOrderRespTypeStr;

            var parameters = new Dictionary<string, string>();
            parameters.Add("symbol", symbolStr);
            parameters.Add("side", sideStr);
            parameters.Add("type", typeStr);
            if (quantity == 0) parameters.Add("quoteOrderQty", quoteOrderQtyStr);
            else parameters.Add("quantity", quantityStr);
            parameters.Add("timestamp", timestampStr);
            parameters.Add("recvWindow", recvWindowStr);
            parameters.Add("newOrderRespType", newOrderRespTypeStr);
            parameters.Add("signature", GenerateSignature(data));

            return PostRequest(BaseEndpoint + endpoint, parameters);
        }

        public object CancelOrder(string symbol, long orderId = 0, string origClientOrderId = "", string newClientOrderId = "",
            int recvWindow = 5000)
        {
            HandleRequestDelay();
            string endpoint = "/api/v3/order";

            string symbolStr = symbol;
            string orderIdStr = orderId.ToString();
            string origClientOrderIdStr = origClientOrderId;
            string newClientOrderIdStr = newClientOrderId;
            string timestampStr = GetCurrentUnixTimeMillisecondsStr();
            string recvWindowStr = (recvWindow > 60000 ? 60000 : recvWindow).ToString();

            string data = "symbol=" + symbolStr +
                (orderId == 0 ? "&origClientOrderId=" + origClientOrderIdStr : "&orderId=" + orderIdStr) +
                (newClientOrderIdStr == "" ? "" : "&newClientOrderId=" + newClientOrderIdStr) +
                "&timestamp=" + timestampStr + "&recvWindow=" + recvWindowStr;

            return DeleteRequest(BaseEndpoint + $"{endpoint}?{data}&signature={GenerateSignature(data)}");
        }

        public object QueryOrder(string symbol, long orderId = 0, string origClientOrderId = "", int recvWindow = 5000)
        {
            HandleRequestDelay();
            string endpoint = "/api/v3/order";

            string symbolStr = symbol;
            string orderIdStr = orderId.ToString();
            string origClientOrderIdStr = origClientOrderId.ToString();
            string timestampStr = GetCurrentUnixTimeMillisecondsStr();
            string recvWindowStr = (recvWindow > 60000 ? 60000 : recvWindow).ToString();

            string data = "symbol=" + symbolStr + (orderId == 0 ? "" : "&orderId=" + orderIdStr) +
                (origClientOrderId == "" ? "" : " &origClientOrderId=" + origClientOrderIdStr) +
                "&timestamp=" + timestampStr + "&recvWindow=" + recvWindowStr;

            return GetRequest(BaseEndpoint + $"{endpoint}?{data}&signature={GenerateSignature(data)}");
        }

        public object QueryOpenOrders(string symbol, int recvWindow = 5000)
        {
            HandleRequestDelay();
            string endpoint = "/api/v3/openOrders";
            string symbolStr = symbol;
            string timestampStr = GetCurrentUnixTimeMillisecondsStr();
            string recvWindowStr = (recvWindow > 60000 ? 60000 : recvWindow).ToString();

            string data = "symbol=" + symbolStr + "&timestamp=" + timestampStr + "&recvWindow=" + recvWindowStr;

            return GetRequest(BaseEndpoint + $"{endpoint}?{data}&signature={GenerateSignature(data)}");
        }

        //-- If orderId is set, it will get orders >= that orderId. Otherwise most recent orders are returned.
        public object QueryAllOrders(string symbol, int orderId = 0, long startTime = 0, long endTime = 0,
            int limit = 500, int recvWindow = 5000)
        {
            HandleRequestDelay();
            string endpoint = "/api/v3/allOrders";

            string symbolStr = symbol;
            string orderIdStr = orderId.ToString();
            string startTimeStr = startTime.ToString();
            string endTimeStr = endTime.ToString();
            string limitStr = (limit > 1000 ? 1000 : limit).ToString();
            string timestampStr = GetCurrentUnixTimeMillisecondsStr();
            string recvWindowStr = (recvWindow > 60000 ? 60000 : recvWindow).ToString();

            string data = "symbol=" + symbolStr + (orderId == 0 ? "" : "&orderId=" + orderIdStr) +
                (startTime == 0 ? "" : "&startTime=" + startTimeStr) +
                (endTime == 0 ? "" : "&endTime=" + endTimeStr) +
                (limit == 0 ? "" : "&limit=" + limitStr) +
                "&timestamp=" + timestampStr + "&recvWindow=" + recvWindowStr;

            return GetRequest(BaseEndpoint + $"{endpoint}?{data}&signature={GenerateSignature(data)}");
        }

        public object GetExchangeInfo()
        {
            HandleRequestDelay();
            string endpoint = "/api/v3/exchangeInfo";
            return GetRequest(BaseEndpoint + endpoint);
        }

        public object PriceTicker(string symbol = "")
        {
            HandleRequestDelay();
            string endpoint = "/api/v3/ticker/price";
            string data = symbol == "" ? "" : "symbol=" + symbol;
            return GetRequest(BaseEndpoint + $"{endpoint}?{data}");
        }


        //-- USER DATA STREAMS

        public object CreateListenKey()
        {
            HandleRequestDelay();
            string endpoint = "/api/v3/userDataStream";
            return PostRequest(BaseEndpoint + endpoint);
        }
        //RESPONSE:
        //
        //{
        //    "listenKey": "pqia91ma19a5s61cv6a81va65sdf19v8a65a1a5s61cv6a81va65sdf19v8a65a1"
        //}

        public object GetTradeFee(string symbol = "", int recvWindow = 5000)
        {
            HandleRequestDelay();
            string endpoint = "/wapi/v3/tradeFee.html";
            string timestampStr = GetCurrentUnixTimeMillisecondsStr();
            string recvWindowStr = (recvWindow > 60000 ? 60000 : recvWindow).ToString();
            string data = (symbol == "" ? "" : "symbol=" + symbol + "&") + "timestamp=" + timestampStr +
                "&recvWindow=" + recvWindowStr;
            return GetRequest(BaseEndpoint + $"{endpoint}?{data}&signature={GenerateSignature(data)}");
        }

        public object RenewListenKey(string listenKey)
        {
            HandleRequestDelay();
            string endpoint = "/api/v3/userDataStream";
            return PutRequest(BaseEndpoint + $"{endpoint}?listenKey={listenKey}");
        }

        public object CloseListenKey(string listenKey)
        {
            HandleRequestDelay();
            string endpoint = "/api/v3/userDataStream";
            return DeleteRequest($"{BaseEndpoint + endpoint}?listenKey={listenKey}");
        }

        //-- REQUEST METHODS

        private object GetRequest(string url)
        {
            HttpRequestMessage msg = new HttpRequestMessage(HttpMethod.Get, url);
            return SendRequestSync(msg);
        }

        private object DeleteRequest(string url)
        {
            HttpRequestMessage msg = new HttpRequestMessage(HttpMethod.Delete, url);
            return SendRequestSync(msg);
        }

        private object PostRequest(string url, Dictionary<string, string> content = null)
        {
            HttpRequestMessage msg = new HttpRequestMessage(HttpMethod.Post, url);
            if (content != null) msg.Content = new FormUrlEncodedContent(content);
            return SendRequestSync(msg);
        }

        private object PutRequest(string url)
        {
            HttpRequestMessage msg = new HttpRequestMessage(HttpMethod.Put, url);
            return SendRequestSync(msg);
        }

        private object SendRequestSync(HttpRequestMessage msg)
        {
            HttpResponseMessage response;

            try
            {
                response = Client.SendAsync(msg).Result;
            }
            catch (Exception e)
            {
                if (e.GetType() == typeof(AggregateException))
                {
                    while (e.InnerException != null)
                        e = e.InnerException;
                }

                throw e;
            }

            object resultJson = null;
            bool responseIsJson = true;

            try
            {
                resultJson = JsonConvert.DeserializeObject(response.Content.ReadAsStringAsync().Result);
            }
            catch (Exception)
            {
                if (response.StatusCode == HttpStatusCode.OK)
                    throw new Exception("Error while deserializing content.");

                responseIsJson = false;
            }

            if (response.StatusCode != HttpStatusCode.OK)
            {
                if (responseIsJson)
                    HandleBinanceErrorResponse(response, resultJson);
                else
                    throw new Exception($"{(int)response.StatusCode} {response.StatusCode}");
            }

            return resultJson;
        }

        private void HandleBinanceErrorResponse(HttpResponseMessage response, dynamic resultJson)
        {
            ErrorCode errorCode;
            string message;

            try
            {
                errorCode = (ErrorCode)int.Parse(resultJson.code.ToString());
                message = resultJson.msg.ToString();
            }
            catch (Exception)
            {
                //-- THROWS AN EXCEPTION WITH A MESSAGE CONTAINING HTTP STATUS CODE
                throw new Exception($"{(int)response.StatusCode} {response.StatusCode + Environment.NewLine + resultJson}");
            }

            //-- THROWS A BINANCEAPIAEXCEPTION IF THE RETURNED JSON CONTENT IS A BINANCE ERROR CODE
            throw new BinanceAPIRequestException(message, errorCode, response.StatusCode);
        }

        public void GetCurrentAccountBalance()
        {
            throw new NotImplementedException();
        }
    }

    public class BinanceAPIRequestException : Exception
    {
        public ErrorCode ErrorCode { get; }
        public HttpStatusCode StatusCode { get; }

        public BinanceAPIRequestException(string message, ErrorCode errorCode, HttpStatusCode statusCode)
            : base(message)
        {
            ErrorCode = errorCode;
            StatusCode = statusCode;
        }
    }
}
