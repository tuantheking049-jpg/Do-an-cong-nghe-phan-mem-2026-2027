
using Backend.Models;

namespace Backend.Repositories;

public class UserRepository
{
    // Lấy danh sách người dùng
    public List<User> GetAll()
    {
        return new List<User>();
    }

    // Lấy người dùng theo ID
    public User? GetById(int id)
    {
        return null;
    }

    // Thêm người dùng
    public void Add(User user)
    {
        // Sẽ triển khai khi kết nối database
    }

    // Cập nhật người dùng
    public void Update(User user)
    {
        // Sẽ triển khai khi kết nối database
    }

    // Xóa người dùng
    public void Delete(int id)
    {
        // Sẽ triển khai khi kết nối database
    }
}
