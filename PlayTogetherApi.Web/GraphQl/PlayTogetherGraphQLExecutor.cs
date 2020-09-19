using System;
using System.Collections.Generic;
using System.Threading;
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
        readonly IServiceProvider _serviceProvider;

        public PlayTogetherGraphQLExecutor(
            PlayTogetherSchema schema,
            IDocumentExecuter documentExecuter,
            IOptions<GraphQLOptions> options,
            IEnumerable<IDocumentExecutionListener> listeners,
            IEnumerable<IValidationRule> validationRules,
            IServiceProvider serviceProvider) :
            base(schema, documentExecuter, options, listeners, validationRules)
        {
            _serviceProvider = serviceProvider;
        }

        protected override ExecutionOptions GetOptions(string operationName, string query, Inputs variables, IDictionary<string, object> context, CancellationToken cancellationToken)
        {
            var options = base.GetOptions(operationName, query, variables, context, cancellationToken);

            options.RequestServices = _serviceProvider;

            return options;
        }
    }
}
