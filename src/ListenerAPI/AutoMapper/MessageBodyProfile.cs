using AutoMapper;
using ListenerAPI.Models;

namespace ListenerAPI.AutoMapper
{
  public class MessageBodyProfile : Profile
  {
    public MessageBodyProfile()
    {
      CreateMap<JobRequest, JobRequestMessageBody>();
    }
  }
}
