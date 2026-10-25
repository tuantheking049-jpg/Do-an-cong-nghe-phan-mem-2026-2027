
package com.doan.backend.service;

import com.doan.backend.dto.RegisterRequest;
import com.doan.backend.model.Role;
import com.doan.backend.model.User;
import com.doan.backend.repository.RoleRepository;
import com.doan.backend.repository.UserRepository;
import org.springframework.security.crypto.bcrypt.BCryptPasswordEncoder;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;
import com.doan.backend.dto.LoginRequest;

@Service
public class AuthService {

    private final UserRepository userRepository;
    private final RoleRepository roleRepository;
    private final BCryptPasswordEncoder passwordEncoder =
            new BCryptPasswordEncoder();

    public AuthService(
            UserRepository userRepository,
            RoleRepository roleRepository) {
        this.userRepository = userRepository;
        this.roleRepository = roleRepository;
    }

    @Transactional
    public String register(RegisterRequest request) {
        if (userRepository.existsByEmail(request.getEmail())) {
            throw new IllegalArgumentException("Email đã được sử dụng");
        }

        Role userRole = roleRepository.findAll()
                .stream()
                .filter(role -> "User".equalsIgnoreCase(role.getRoleName()))
                .findFirst()
                .orElseThrow(() ->
                        new IllegalStateException("Chưa có vai trò User"));

        User user = new User();
        user.setFullName(request.getFullName());
        user.setEmail(request.getEmail());
        user.setPasswordHash(
                passwordEncoder.encode(request.getPassword()));
        user.setRole(userRole);

        userRepository.save(user);

        return "Đăng ký tài khoản thành công";
    }

    public String login(LoginRequest request) {
    User user = userRepository.findByEmail(request.getEmail())
            .orElseThrow(() ->
                    new IllegalArgumentException(
                            "Email hoặc mật khẩu không chính xác"));

    if (!passwordEncoder.matches(
            request.getPassword(),
            user.getPasswordHash())) {
        throw new IllegalArgumentException(
                "Email hoặc mật khẩu không chính xác");
    }

    return "Đăng nhập thành công";
    }
}
