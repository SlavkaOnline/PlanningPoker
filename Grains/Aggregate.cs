using System;
using System.Threading.Tasks;
using Microsoft.FSharp.Core;
using PlanningPoker.Domain;

namespace Grains
{
    internal class Aggregate<TState, TCommand, TEvent>
    {
		private readonly Func<TState, TCommand, FSharpResult<TEvent, Errors>> _producer;
		private readonly Func<TEvent, Task> _commiter;

        public Aggregate(
            Func<TState, TCommand, FSharpResult<TEvent, Errors>> producer,
            Func<TEvent, Task> commiter)
        {
			_producer = producer;
			_commiter = commiter;
        }

        public async Task Exec(TState state, TCommand command)
        {
            var result = _producer(state, command);
            if (result.IsOk)
            {
                await _commiter(result.ResultValue);
            }
            else
            {
                Errors.RaiseDomainExn<Errors>(result.ErrorValue);
            }
        }
    }
}