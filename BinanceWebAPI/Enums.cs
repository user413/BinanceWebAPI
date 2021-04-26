namespace BinanceWebAPI
{
    public enum OrderType
    {
        LIMIT,
        MARKET,
        STOP_LOSS,
        STOP_LOSS_LIMIT,
        TAKE_PROFIT,
        TAKE_PROFIT_LIMIT,
        LIMIT_MAKER
    }
    public enum OrderSide
    {
        BUY, SELL
    }
    public enum OrderTimeInForce
    {
        GTC, IOC, FOK
    }
    public enum OrderRespType
    {
        ACK, RESULT, FULL
    }

    public enum APIErrorCode
    {
        NO_SUCH_ORDER = -2013,
        INVALID_TIMESTAMP = -1021,
        BALANCE_NOT_ENOUGH = -6012,
        CANCEL_REJECTED = -2011,
        REJECTED_MBX_KEY = -2015,
        INVALID_SIGNATURE = -1022
    }

    public enum OrderStatus
    {
        NEW,
        PARTIALLY_FILLED,
        FILLED,
        CANCELED,
        PENDING_CANCEL,
        REJECTED,
        EXPIRED
    }
}
