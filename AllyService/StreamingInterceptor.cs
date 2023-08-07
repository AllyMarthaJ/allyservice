using Grpc.Core;
using Grpc.Core.Interceptors;

namespace AllyService; 

public class StreamingInterceptor : Interceptor {
    public override AsyncServerStreamingCall<TResponse> AsyncServerStreamingCall<TRequest, TResponse>(
        TRequest request, ClientInterceptorContext<TRequest, TResponse> context,
        AsyncServerStreamingCallContinuation<TRequest, TResponse> continuation) {

        var headers = new Metadata();
        headers.Add(new Metadata.Entry("Content-Type", "text/event-stream"));

        var newOptions = context.Options.WithHeaders(headers);

        var newContext = new ClientInterceptorContext<TRequest, TResponse>(context.Method, context.Host, newOptions);
        
        Console.WriteLine("HELLO");

        return continuation(request, newContext);
    }
}