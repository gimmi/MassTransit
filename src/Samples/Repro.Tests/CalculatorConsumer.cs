namespace Repro.Tests
{
    using System.Threading.Tasks;
    using MassTransit;


    public class CalculatorConsumer : IConsumer<SumRequest>
    {
        public async Task Consume(ConsumeContext<SumRequest> context)
        {
            var request = context.Message;
            await context.RespondAsync(new SumResponse {
                Sum = request.Value1 + request.Value2
            });
        }
    }
}