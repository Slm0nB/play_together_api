using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GraphQL;
using GraphQL.Execution;
using GraphQL.Server;
using GraphQL.Server.Internal;
using GraphQL.Validation;
using Microsoft.Extensions.Options;

namespace PlayTogetherApi.Web.GraphQl
{
    public class PlayTogetherGraphQLExecutor : DefaultGraphQLExecuter<PlayTogetherSchema>
    {
        public PlayTogetherGraphQLExecutor(
            PlayTogetherSchema schema,
            IDocumentExecuter documentExecuter,
            IOptions<GraphQLOptions> options,
            IEnumerable<IDocumentExecutionListener> listeners,
            IEnumerable<IValidationRule> validationRules) :
            base(schema, documentExecuter, options, listeners, validationRules)
        {
        }

        /*
        public override Task<ExecutionResult> ExecuteAsync(string operationName, string query, Inputs variables, IDictionary<string, object> context, IServiceProvider requestServices, CancellationToken cancellationToken = default)
        {
            return base.ExecuteAsync(operationName, query, variables, context, requestServices, cancellationToken);
        }

        protected override ExecutionOptions GetOptions(string operationName, string query, Inputs variables, IDictionary<string, object> context, IServiceProvider requestServices, CancellationToken cancellationToken)
        {
            return base.GetOptions(operationName, query, variables, context, requestServices, cancellationToken);
        }
        */
    }
}
