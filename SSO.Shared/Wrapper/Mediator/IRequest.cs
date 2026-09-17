using System;
using System.Collections.Generic;
using System.Text;

namespace SSO.Shared.Wrapper.Mediator
{
    public interface IRequest<out TResponse> { }
    public interface IRequest { }
    public interface INotification { }
    public interface IStreamRequest<out TResponse> { }
}
