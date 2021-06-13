using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Azure;
using Azure.Data.Tables;
using GitHubNotifications.Models;

namespace GitHubNotifications.Server
{
    public static class Actions
    {
        public static async Task<bool> AllCommentsForPR(Func<PRComment, PullRequest, TableTransactionAction> action, PullRequest pr, TableClient commentTable)
        {
            var actions = new List<TableTransactionAction>();
            var number = pr.Number.ToString();
            await foreach (var comment in GetComments(pr.User.Login, number, commentTable))
            {
                actions.Add(action(comment, pr));
            }
            await ExecuteActionsAsBatch(actions, commentTable);
            return true;
        }

        private static async Task ExecuteActionsAsBatch(List<TableTransactionAction> actions, TableClient commentTable)
        {
            int skip = 0;
            while (skip < actions.Count)
            {
                await commentTable.SubmitTransactionAsync(actions.Skip(skip).Take(100));
                skip += 100;
            }
        }

        private static AsyncPageable<PRComment> GetComments(string author, string number, TableClient commentTable) =>
            commentTable.QueryAsync<PRComment>(e => e.PartitionKey == author && e.PrNumber == number);
    }
}