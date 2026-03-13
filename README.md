## BinanceWebAPI
- Synchronous methods only
- Request requirements set through method parameters
- All responses are returned as the corresponding json type from Newtonsoft.Json library (JObject and JArray), having the same structures as defined in the official Binance API documentation
- Throws its own exceptions, as defined below, other than connection related native .NET exceptions<div style="font-size:10px;">

#### This library currently contains:
  - Most of the rest API essential methods for spot, cross margin and isolated margin trading:
    - Placing and retrieving order information (limit and market)
    - Asset balances information
    - Connection to websocket (user data streams)
  - Exchange information