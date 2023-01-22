﻿using Microsoft.EntityFrameworkCore;
using System.ComponentModel;
using System.Diagnostics;
using TraJediServer.Journal;

namespace TraJedi.Journal.Data.Services
{
    public class TradesRepository : ITradesRepository
    {
        private readonly TradingJournalDataContext dataContext;

        #region Ctor

        public TradesRepository(TradingJournalDataContext journalDbContext)
        {
            dataContext = journalDbContext ?? throw new ArgumentNullException(nameof(journalDbContext));

        }
        #endregion

        #region Trades 

        public async Task<IEnumerable<TradeInputModel>> GetAllTradeOverviewsAsync()
        {
            var _oneLiners = new List<TradeInputModel>();

            foreach (var trade in dataContext.OverallTrades)
            {
                TradeInputModel tradeSummary = new TradeInputModel()
                {
                    TradeInputType = TradeInputType.Overview1Liner,
                    TradeInputComponents = new List<InputComponentModel>()
                };

                tradeSummary.TradeInputComponents = await dataContext.TradeInputComponents
                  .Where(tc => tc.TradeInputModel.TradeConstructId == trade.Id && tc.IsRelevantForOverview)
                  .OrderBy(tc => tc.Id)
                  .ToListAsync();

                _oneLiners.Add(tradeSummary);
            }

            return _oneLiners;
        }

        public async Task<TradeConstruct> AddTradeAsync()
        {
            TradeConstruct trade = new TradeConstruct();
            trade.TradeInputs.Add(new TradeInputModel()
            {
                TradeInputType = TradeInputType.Origin,
                TradeInputComponents = ComponentListsFactory.GetTradeOriginComponents()
            });
            await dataContext.OverallTrades.AddAsync(trade);
            await dataContext.SaveChangesAsync();

            return trade;
        }

        public async Task<(IEnumerable<TradeConstruct>, PaginationMetadata)> GetAllTradesAsync(int pageNumber = 1, int pageSize = 10)
        {
            //collection to start from
            var collection = dataContext.OverallTrades as IQueryable<TradeConstruct>;

            var totalItemCount = await collection.CountAsync();

            var paginationMetadata = new PaginationMetadata(totalItemCount, pageSize, pageNumber);

            var collectionToReturn = await collection.OrderBy(t => t.CreatedAt)
                .Skip(pageSize * (pageNumber - 1))
                .Take(pageSize)
                .ToListAsync();

            return (collectionToReturn, paginationMetadata);
        }

        #endregion

        #region Trade Inputs 

        #region Getters

        public async Task<TradeInputModel?> GetTradeOriginAsync(string tradeId)
        {
            return await GetTradeInputByTypeAsync(tradeId, TradeInputType.Origin);
        }

        public async Task<TradeInputModel?> GetTradeClosureAsync(string tradeId)
        {
            return await GetTradeInputByTypeAsync(tradeId, TradeInputType.Closure);
        }

        #endregion

        #region add/remove

        public async Task<(TradeInputModel newEntry, TradeInputModel summary)> NewEntryAddPositionAsync(string tradeId)
        {
            TradeInputModel tradeInput = new TradeInputModel()
            {
                TradeInputType = TradeInputType.Interim,
                TradeInputComponents = ComponentListsFactory.GetAddToPositionComponents(isActual: true)
            };

            return await AddInterimTradeInputAsync(tradeId, tradeInput);
        }

        public async Task<(TradeInputModel newEntry, TradeInputModel summary)> NewEntryReducePositionAsync(string tradeId)
        {
            TradeInputModel tradeInput = new TradeInputModel()
            {
                TradeInputType = TradeInputType.Interim,
                TradeInputComponents = ComponentListsFactory.GetReducePositionComponents()
            };

            return await AddInterimTradeInputAsync(tradeId, tradeInput);
        }

        public async Task<(bool result, TradeInputModel? summary)> RemoveInterimEntry(string tradeInputId)
        {
            bool res = await RemoveInterimInputAsync(tradeInputId);
            TradeInputModel? summary = null;

            if (res)
            {
                summary = await UpdateInterimSummaryAsync(tradeInputId);
                await dataContext.SaveChangesAsync();
            }

            return (res, summary);
        }

        #endregion

        public async Task<InputComponentModel?> UpdateTradeInputComponent(string componentId, string newContent, string changeNote)
        {
            InputComponentModel? inputComponent = await dataContext.TradeInputComponents.Where(t => t.Id.ToString() == componentId).FirstOrDefaultAsync();
            if (inputComponent != null)
            {
                inputComponent.History.Add(inputComponent.ContentWrapper);
                inputComponent.ContentWrapper = new ContentModel() { Content = newContent, ChangeNote = changeNote };

                await UpdateInterimSummaryAsync(inputComponent.TradeInputModel.TradeConstructId.ToString());

                await dataContext.SaveChangesAsync();
            }

            return inputComponent;
        }

        #endregion

        #region Closure

        public async Task Close(string tradeId, string closingPrice)
        {
            var analytics = await GetAvgEntryAndProfitAsync(tradeId);

            #region add reduction for current amount at specified price
            TradeInputModel tradeInput = new TradeInputModel()
            {
                TradeInputType = TradeInputType.Interim,
                TradeInputComponents = ComponentListsFactory.GetReducePositionComponents()
            };

            InputComponentModel? price = tradeInput.TradeInputComponents.Where(ti => ti.PriceRelevance == ValueRelevance.Substract).FirstOrDefault();
            if (price == null)
                throw new Exception("could not find price entry to reduce / close position");
            else
            {
                price.Content = closingPrice;
            }

            InputComponentModel? cost = tradeInput.TradeInputComponents.Where(ti => ti.CostRelevance == ValueRelevance.Substract).FirstOrDefault();
            if (cost == null)
                throw new Exception("could not find cost entry to reduce / close position");
            else if (double.TryParse(closingPrice, out double amountValue))
            {
                cost.Content = (amountValue * analytics.totalAmount).ToString();
            }
            else
                throw new Exception("could not parse closing price to close position");


            await dataContext.TradeInputs.AddAsync(tradeInput);
            #endregion

            // remove interim summary

            await RemoveInterimInputAsync(TradeInputType.InterimSummary);

            // create actual closure

            analytics = await GetAvgEntryAndProfitAsync(tradeId);
            TradeInputModel tradeClosure = new TradeInputModel()
            {
                TradeInputType = TradeInputType.Closure,
                TradeInputComponents = ComponentListsFactory.GetTradeClosureComponents(profitValue: analytics.profit.ToString())
            };

            await dataContext.TradeInputs.AddAsync(tradeClosure);

            await dataContext.SaveChangesAsync();
        }

        #endregion

        #region Helpers

        public async Task<TradeInputModel?> GetTradeInputByTypeAsync(string tradeId, TradeInputType tradeInputType)
        {
            return await dataContext.TradeInputs
               .Where(t => t.TradeConstructId.ToString() == tradeId && t.TradeInputType == tradeInputType)
               .FirstOrDefaultAsync();
        }

        private async Task<bool> RemoveInterimInputAsync(string tradeInputId)
        {
            TradeInputModel? tradeInput =
                 await dataContext.TradeInputs.Where(t => t.Id.ToString() == tradeInputId).FirstOrDefaultAsync();

            if (tradeInput != null && tradeInput.TradeInputType == TradeInputType.Interim)
            {
                dataContext.TradeInputs.Remove(tradeInput);
                return true;
            }

            return false;
        }

        private async Task<bool> RemoveInterimInputAsync(TradeInputType tradeInputType)
        {
            TradeInputModel? tradeInput =
                 await dataContext.TradeInputs.Where(t => t.TradeInputType == tradeInputType).FirstOrDefaultAsync();

            if (tradeInput != null)
            {
                dataContext.TradeInputs.Remove(tradeInput);
                return true;
            }

            return false;
        }

        private async Task<(TradeInputModel newEntry, TradeInputModel summary)> AddInterimTradeInputAsync(string tradeInputId, TradeInputModel tradeInput)
        {
            await dataContext.TradeInputs.AddAsync(tradeInput);
            TradeInputModel summary = await UpdateInterimSummaryAsync(tradeInputId);

            await dataContext.SaveChangesAsync();

            return (tradeInput, summary);
        }

        private async Task<(double totalCost, double totalAmount, double profit)> GetAvgEntryAndProfitAsync(string tradeId)
        {
            List<(double priceValue, double cost)> entriesWithAmount = new();
            double profit = 0.0;

            var interims = await dataContext.TradeInputs
                .Where(t => t.TradeConstructId.ToString() == tradeId && t.TradeInputType == TradeInputType.Interim)
                .ToListAsync();

            foreach (var tradeInput in interims)
            {
                double cost = 0.0;
                double priceValue = 0.0;
                foreach (var component in tradeInput.TradeInputComponents)
                {
                    if (component.CostRelevance == ValueRelevance.Add || component.CostRelevance == ValueRelevance.Substract)
                    {
                        double.TryParse(component.ContentWrapper.Content, out cost);

                        if (component.CostRelevance == ValueRelevance.Add)
                        {
                            profit += cost;
                        }

                        else if (component.CostRelevance == ValueRelevance.Substract)
                        {
                            profit -= cost;
                        }
                    }

                    if (component.PriceRelevance == ValueRelevance.Add || component.PriceRelevance == ValueRelevance.Substract)
                    {
                        double.TryParse(component.ContentWrapper.Content, out priceValue);
                        if (component.PriceRelevance == ValueRelevance.Substract)
                        {
                            priceValue *= -1;
                        }
                    }
                }

                //should have both change and price now
                if (cost > 0 && priceValue > 0)
                {
                    entriesWithAmount.Add((priceValue, cost));
                }
            }


            double totalAmount = 0.0;
            double totalCost = 0.0;

            foreach (var item in entriesWithAmount)
            {
                //will substract if exit trade
                totalCost += item.cost * item.priceValue;
                totalAmount += item.cost / item.priceValue;
            }

            return (totalCost, totalAmount, profit);
        }


        //summary

        private async Task<TradeInputModel> AddInterimSummaryAsync(string tradeId)
        {
            var analytics = await GetAvgEntryAndProfitAsync(tradeId);

            string averageEntry = string.Empty, totalAmount = string.Empty, totalCost = string.Empty;
            if (analytics.totalCost > 0)
            {
                totalCost = analytics.totalCost.ToString();

                if (analytics.totalAmount > 0)
                {
                    totalAmount = analytics.totalAmount.ToString();
                    averageEntry = (analytics.totalCost / analytics.totalAmount).ToString();
                }
            }

            TradeInputModel tradeInput = new TradeInputModel()
            {
                TradeInputType = TradeInputType.Interim,
                TradeInputComponents = ComponentListsFactory.GetSummaryComponents(averageEntry, totalAmount, totalCost)
            };

            await dataContext.TradeInputs.AddAsync(tradeInput);
            return tradeInput;
        }

        private async Task<TradeInputModel> UpdateInterimSummaryAsync(string tradeInputId)
        {
            await RemoveInterimInputAsync(TradeInputType.InterimSummary);
            return await AddInterimSummaryAsync(tradeInputId);
        }

        #endregion
    }
}