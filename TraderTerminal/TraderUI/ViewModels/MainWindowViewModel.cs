using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using ReactiveUI;
using Avalonia.Threading;
using Trader;
using Trader.Clients;
using Trader.Interfaces;
using Trader.Models;
using Trader.Services;
using TraderUI.Models;

namespace TraderUI.ViewModels
{
    public class MainWindowViewModel : ReactiveObject
    {
        public ObservableCollection<PortfolioAsset> PortfolioAssets { get; } = new();
        public ObservableCollection<PortfolioResult> PortfolioResults { get; } = new();
        
        public ReactiveCommand<Unit, Unit> CalculatePortfolioCommand { get; }
        
        public ObservableCollection<Trade> BuyTrades { get; } = new();
        public ObservableCollection<Trade> SellTrades { get; } = new();
        
        private string _tradesPair = "BTCUSD";
        public string TradesPair
        {
            get => _tradesPair;
            set => this.RaiseAndSetIfChanged(ref _tradesPair, value);
        }

        private int _tradesMaxCount = 100;
        public int TradesMaxCount
        {
            get => _tradesMaxCount;
            set => this.RaiseAndSetIfChanged(ref _tradesMaxCount, value);
        }

        public ReactiveCommand<Unit, Unit> SubscribeTradesCommand { get; }
        public ReactiveCommand<Unit, Unit> UnsubscribeTradesCommand { get; }
        public ReactiveCommand<Unit, Unit> GetTradesCommand { get; }
        
        public ObservableCollection<Candle> Candles { get; } = new();

        private string _candlesPair = "BTCUSD";
        public string CandlesPair
        {
            get => _candlesPair;
            set => this.RaiseAndSetIfChanged(ref _candlesPair, value);
        }

        private int _candlesPeriodInSec = 60;
        public int CandlesPeriodInSec
        {
            get => _candlesPeriodInSec;
            set => this.RaiseAndSetIfChanged(ref _candlesPeriodInSec, value);
        }

        private int _candlesCount = 20;
        public int CandlesCount
        {
            get => _candlesCount;
            set => this.RaiseAndSetIfChanged(ref _candlesCount, value);
        }

        public ReactiveCommand<Unit, Unit> SubscribeCandlesCommand { get; }
        public ReactiveCommand<Unit, Unit> UnsubscribeCandlesCommand { get; }
        public ReactiveCommand<Unit, Unit> GetCandlesCommand { get; }
        
        private readonly IPortfolioCalculator _portfolioCalculator;
        private readonly IConnector _connector;

        public MainWindowViewModel()
        {
            IRestClient restClient = new BitfinexRestClient();
            IWsClient wsClient = new BitfinexWsClient();
            
            _portfolioCalculator = new PortfolioCalculator(new CurrencyConverter(restClient));
            _connector = new Connector(wsClient, restClient);
            
            PortfolioAssets.Add(new PortfolioAsset { Symbol = "BTC", Amount = 1m });
            PortfolioAssets.Add(new PortfolioAsset { Symbol = "XRP", Amount = 15000m });
            PortfolioAssets.Add(new PortfolioAsset { Symbol = "XMR", Amount = 50m });
            PortfolioAssets.Add(new PortfolioAsset { Symbol = "DASH", Amount = 30m });
            
            CalculatePortfolioCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                PortfolioResults.Clear();
                
                var portfolioDict = PortfolioAssets.ToDictionary(
                    asset => asset.Symbol,
                    asset => asset.Amount
                );
                
                var targetCurrencies = new[] { "USDT", "BTC", "XRP", "XMR", "DASH" };
                
                var result = await _portfolioCalculator.CalculatePortfolioValuesAsync(portfolioDict, targetCurrencies);
                
                foreach (var kvp in result)
                {
                    PortfolioResults.Add(new PortfolioResult
                    {
                        TargetCurrency = kvp.Key,
                        TotalValue = kvp.Value
                    });
                }
            });
            
            CalculatePortfolioCommand.ThrownExceptions.Subscribe(ex =>
            {
                Console.WriteLine("CalculatePortfolioCommand: " + ex);
            });
            
            _connector.NewBuyTrade += trade =>
                Dispatcher.UIThread.Post(() => BuyTrades.Add(trade));
            _connector.NewSellTrade += trade =>
                Dispatcher.UIThread.Post(() => SellTrades.Add(trade));
            _connector.CandleSeriesProcessing += candle =>
                Dispatcher.UIThread.Post(() => Candles.Add(candle));
            
            SubscribeTradesCommand = ReactiveCommand.Create(() =>
            {
                _connector.SubscribeTrades(TradesPair, TradesMaxCount);
            });
            UnsubscribeTradesCommand = ReactiveCommand.Create(() =>
            {
                _connector.UnsubscribeTrades(TradesPair);
            });
            GetTradesCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                try
                {
                    var trades = await _connector.GetNewTradesAsync(TradesPair, TradesMaxCount);
                    BuyTrades.Clear();
                    SellTrades.Clear();

                    foreach (var trade in trades)
                    {
                        if (trade.Side.Equals("buy", StringComparison.OrdinalIgnoreCase))
                            BuyTrades.Add(trade);
                        else
                            SellTrades.Add(trade);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("GetTradesCommand: " + ex);
                }
            });
            GetTradesCommand.ThrownExceptions.Subscribe(ex =>
            {
                Console.WriteLine("GetTradesCommand ThrownExceptions: " + ex);
            });
            
            SubscribeCandlesCommand = ReactiveCommand.Create(() =>
            {
                _connector.SubscribeCandles(CandlesPair, CandlesPeriodInSec);
            });
            UnsubscribeCandlesCommand = ReactiveCommand.Create(() =>
            {
                _connector.UnsubscribeCandles(CandlesPair);
            });
            GetCandlesCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                try
                {
                    var newCandles = await _connector.GetCandleSeriesAsync(
                        CandlesPair,
                        CandlesPeriodInSec,
                        from: null,
                        count: CandlesCount,
                        to: null
                    );

                    Candles.Clear();
                    foreach (var c in newCandles)
                    {
                        Candles.Add(c);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("GetCandlesCommand: " + ex);
                }
            });
            GetCandlesCommand.ThrownExceptions.Subscribe(ex =>
            {
                Console.WriteLine("GetCandlesCommand ThrownExceptions: " + ex);
            });
        }
    }
}
