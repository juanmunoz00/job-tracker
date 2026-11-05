using JobTracker.Core.Models;

namespace JobTracker.Core.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByUsernameAsync(string username);
}