using AutoMapper;
using Azure.Messaging.ServiceBus;
using ListenerAPI.Models;

namespace ListenerAPI.AutoMapper
{
  public class ReceivedMessageProfile : Profile
  {
    public ReceivedMessageProfile()
    {
      CreateMap<ServiceBusReceivedMessage, ReceivedMessage>();
    }
  }
}
