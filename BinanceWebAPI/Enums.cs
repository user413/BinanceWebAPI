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
    public enum MarketOrderQtyType
    {
        Base, Quote
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

    public enum ChartInterval
    {
        ONE_MIN,
        THREE_MIN,
        FIVE_MIN,
        FIFTEEN_MIN,
        THIRTY_MIN,
        ONE_HOUR,
        TWO_HOUR,
        FOUR_HOUR,
        SIX_HOUR,
        EIGHT_HOUR,
        TWELVE_HOUR,
        ONE_DAY,
        THREE_DAY,
        ONE_WEEK,
        ONE_MONTH
    }

    public enum CSDataProperty
    {
        OpenTime,
        Open,
        High,
        Low,
        Close,
        Volume,
        CloseTime,
        QuoteAssetVolume,
        NumberOfTrades,
        TakerBuyBaseAssetVolume,
        TakerBuyQuoteAssetVolume,
        Ignore
    }
}
