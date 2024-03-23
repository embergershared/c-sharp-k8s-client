namespace ListenerAPI.Factories
{
  public interface IAbstractFactory<out T>
  {
    T Create();
  }
}
