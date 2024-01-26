namespace SimpleApi.Support;

public interface IUnitOfWork
{
    Task<bool> CompleteAsync();
}