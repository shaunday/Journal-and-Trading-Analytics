﻿using DayJT.Journal.Data;
using DayJTrading.Journal.Data;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.ComponentModel;
using System.Diagnostics;

namespace DayJT.Journal.DataContext.Services
{
    public class JournalRepository : IJournalRepository
    {
        private readonly TradingJournalDataContext dataContext;

        #region Ctor

        public JournalRepository(TradingJournalDataContext journalDbContext)
        {
            dataContext = journalDbContext ?? throw new ArgumentNullException(nameof(journalDbContext));

        }
        #endregion

        #region Trade Composite 

        public async Task<TradeComposite> AddTradeCompositeAsync()
        {
            TradeComposite trade = new TradeComposite();
            try
            {
                TradeElement originElement = new TradeElement(trade, TradeActionType.Origin);
                trade.TradeElements.Add(originElement);

                dataContext.AllTradeComposites.Add(trade);
                await dataContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                string t = ex.ToString();
            }
            return trade;
        }

        public async Task<(IEnumerable<TradeComposite>, PaginationMetadata)> GetAllTradeCompositesAsync(int pageNumber = 1, int pageSize = 10)
        {
            //collection to start from
            var collection = dataContext.AllTradeComposites as IQueryable<TradeComposite>;

            var totalItemCount = await collection.CountAsync();

            var paginationMetadata = new PaginationMetadata(totalItemCount, pageSize, pageNumber);

            var collectionToReturn = await collection.OrderBy(t => t.CreatedAt)
                .Skip(pageSize * (pageNumber - 1))
                .Take(pageSize)
                .ToListAsync();

            return (collectionToReturn, paginationMetadata);
        }

        #endregion

        #region Trade Elements 

        public async Task<(TradeElement newEntry, TradeElement summary)> AddInterimPositionAsync(string tradeId, bool isAdd)
        {
            var trade = await GetTradeCompositeAsync(tradeId);
            TradeElement tradeInput = new TradeElement(trade, isAdd? TradeActionType.AddPosition : TradeActionType.ReducePosition);

            trade.TradeElements.Add(tradeInput);
            TradeElement newSummary = JournalRepoHelpers.GetInterimSummary(trade);

            trade.Summary = newSummary;
            await dataContext.SaveChangesAsync();

            return (tradeInput, newSummary);
        }

        public async Task<TradeElement?> RemoveInterimPositionAsync(string tradeId, string tradeInputId)
        {
            var trade = await GetTradeCompositeAsync(tradeId);
            if (trade.TradeElements.Count <= 1)
            {
                throw new InvalidOperationException($"No entries to remove on trade ID {tradeId} .");
            }
            JournalRepoHelpers.RemoveInterimInput(ref trade, tradeInputId);
            TradeElement summary = JournalRepoHelpers.GetInterimSummary(trade);
            trade.Summary = summary;

            await dataContext.SaveChangesAsync();

            return summary;
        }

        #endregion

        #region Entries Update

        public async Task<(Cell updatedCell, TradeElement? summary)> UpdateCellContent(string componentId, string newContent, string changeNote)
        {
            var cell = await dataContext.AllEntries.Where(t => t.Id.ToString() == componentId).SingleOrDefaultAsync();
            if (cell == null)
            {
                throw new InvalidOperationException($"Entry with ID {componentId} not found.");
            }

            cell.SetFollowupContent(newContent, changeNote);

            TradeElement? summary = null;
            if (cell.IsRelevantForOverview)
            {
                var trade = cell.TradeElementRef.TradeCompositeRef;
                summary = JournalRepoHelpers.GetInterimSummary(trade);
                trade.Summary = summary;
            }

            await dataContext.SaveChangesAsync();

            return (cell, summary);
        }

        #endregion

        #region Closure

        public async Task<TradeElement> CloseAsync(string tradeId, string closingPrice)
        {
            var trade = await GetTradeCompositeAsync(tradeId);
            var analytics = JournalRepoHelpers.GetAvgEntryAndProfit(trade);

            // Create and add reduction entry for closing
            var tradeInput = JournalRepoHelpers.CreateTradeElementForClosure(trade, closingPrice, analytics);
            trade.TradeElements.Add(tradeInput);

            // Generate summary based on updated trade
            trade.Summary = JournalRepoHelpers.GetInterimSummary(trade);

            await dataContext.SaveChangesAsync();

            return trade.Summary!;
        }

        #endregion

        private async Task<TradeComposite> GetTradeCompositeAsync(string tradeId)
        {
            var trade = await dataContext.AllTradeComposites.Where(t => t.Id.ToString() == tradeId).SingleOrDefaultAsync();

            if (trade == null)
            {
                throw new InvalidOperationException($"Trade with ID {tradeId} not found.");
            }

            return trade!;
        }
    }
}
