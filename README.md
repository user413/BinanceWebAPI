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
  - Essential exchange information
#### Exceptions:
|Type|Description|Properties|
|:------------ |:------------|-|
|BinanceAPIException|Occurs when a request reached the exchange and an error code is returned, representing specific API errors|Http status code, API error code|
|ResponseException|Occurs when a request is sent and a response is retrieved with an error, but doesn't represent an error code from the exchange|Http status code, response text|