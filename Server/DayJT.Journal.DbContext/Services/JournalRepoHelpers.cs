﻿using DayJT.Journal.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DayJT.Journal.DataContext.Services
{
    internal static class JournalRepoHelpers
    {
        internal static (double totalCost, double totalAmount, double profit) GetAvgEntryAndProfit(TradeComposite trade)
        {
            List<(double priceValue, double cost)> entriesWithAmount = new();
            double profit = 0.0;

            var interims = trade.TradeElements
                .Where(t => t.TradeActionType == TradeActionType.Interim)
                .ToList();

            foreach (var tradeInput in interims)
            {
                double cost = 0.0;
                double priceValue = 0.0;
                foreach (var component in tradeInput.Entries)
                {
                    if (component.CostRelevance == ValueRelevance.Add || component.CostRelevance == ValueRelevance.Substract)
                    {
                        double.TryParse(component.Content, out cost);

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
                        double.TryParse(component.Content, out priceValue);
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

        internal static TradeElement? GetInterimSummary(TradeComposite trade)
        {
            TradeElement? summary = null;
            if (trade != null)
            {
                var analytics = GetAvgEntryAndProfit(trade);

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


                summary = new TradeElement(trade)
                {
                    TradeActionType = TradeActionType.InterimSummary,
                    Entries = TradeElementFactory.GetSummaryComponents(averageEntry, totalAmount, totalCost)
                };
            }

            return summary;
        }

        internal static bool RemoveInterimInput(TradeComposite trade, string tradeInputId)
        {
            if (trade != null)
            {
                var tradeInputToRemove = trade.TradeElements.Where(t => t.Id.ToString() == tradeInputId).SingleOrDefault();

                if (tradeInputToRemove != null && tradeInputToRemove.TradeActionType == TradeActionType.Interim)
                {
                    trade.TradeElements.Remove(tradeInputToRemove);
                    return true;
                }
            }

            return false;
        }

        internal static bool RemoveInterimInput(TradeComposite trade, TradeActionType tradeInputType)
        {
            if (trade != null)
            {
                var tradeInputToRemove = trade.TradeElements.Where(t => t.TradeActionType == tradeInputType).SingleOrDefault();

                if (tradeInputToRemove != null)
                {
                    trade.TradeElements.Remove(tradeInputToRemove);
                    return true;
                }
            }

            return false;
        }
    }
}