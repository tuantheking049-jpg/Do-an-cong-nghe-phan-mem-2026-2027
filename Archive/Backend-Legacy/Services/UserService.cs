
using Backend.Models;
using Backend.Repositories;

namespace Backend.Services;

public class UserService
{
    private readonly UserRepository _userRepository;

    public UserService(UserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public List<User> GetAll()
    {
        return _userRepository.GetAll();
    }

    public User? GetById(int id)
    {
        if (id <= 0)
        {
            return null;
        }

        return _userRepository.GetById(id);
    }

    public void Add(User user)
    {
        _userRepository.Add(user);
    }

    public void Update(User user)
    {
        _userRepository.Update(user);
    }

    public void Delete(int id)
    {
        _userRepository.Delete(id);
    }
}
