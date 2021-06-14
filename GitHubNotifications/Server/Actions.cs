using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Azure;
using Azure.Data.Tables;
using GitHubNotifications.Models;
using Microsoft.Extensions.Logging;

namespace GitHubNotifications.Server
{
    public static class Actions
    {
        public static async Task<bool> AllCommentsForPR(Func<PRComment, PullRequest, TableTransactionAction> action, PullRequest pr, TableClient commentTable, ILogger logger)
        {
            var actions = new List<TableTransactionAction>();
            var number = pr.Number.ToString();
            var filter = TableClient.CreateQueryFilter<PRComment>(e => e.PartitionKey == pr.User.Login && e.PrNumber == number);
            logger.LogInformation($"Querying comments with filter {filter}");
            await foreach (var comment in commentTable.QueryAsync<PRComment>(e => e.PartitionKey == pr.User.Login && e.PrNumber == number))
            {
                actions.Add(action(comment, pr));
            }
            logger.LogInformation($"GetComments generated {actions.Count} actions");
            await ExecuteActionsAsBatch(actions, commentTable, logger);
            return true;
        }

        private static async Task ExecuteActionsAsBatch(List<TableTransactionAction> actions, TableClient commentTable, ILogger logger)
        {
            int skip = 0;
            while (skip < actions.Count)
            {
                logger.LogInformation($"Submitting transactions with skip: {skip}, Count: {actions.Skip(skip).Take(100).Count()}");
                await commentTable.SubmitTransactionAsync(actions.Skip(skip).Take(100));
                logger.LogInformation($"Submitted transactions with skip: {skip}, Count: {actions.Skip(skip).Take(100).Count()}");
                skip += 100;
            }
        }

        private static AsyncPageable<PRComment> GetComments(string author, string number, TableClient commentTable) =>
            commentTable.QueryAsync<PRComment>(e => e.PartitionKey == author && e.PrNumber == number);
    }
}