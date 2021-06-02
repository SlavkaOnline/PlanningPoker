using System;
using System.Threading.Tasks;
using Microsoft.FSharp.Core;
using PlanningPoker.Domain;

namespace Grains
{
    internal class Aggregate<TState, TCommand, TEvent>
    {
		private readonly Func<TState, TCommand, FSharpResult<TEvent, Errors>> _producer;
		private readonly Func<TEvent, Task> _committer;

        public Aggregate(
            Func<TState, TCommand, FSharpResult<TEvent, Errors>> producer,
            Func<TEvent, Task> committer)
        {
			_producer = producer;
            _committer = committer;
        }

        public async Task Exec(TState state, TCommand command)
        {
            var result = _producer(state, command);
            if (result.IsOk)
            {
                await _committer(result.ResultValue);
            }
            else
            {
                Errors.RaiseDomainExn<Errors>(result.ErrorValue);
            }
        }
    }
}