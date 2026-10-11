
package com.doan.backend.controller;

import com.doan.backend.dto.LoginRequest;
import com.doan.backend.dto.RegisterRequest;
import com.doan.backend.service.AuthService;
import jakarta.validation.Valid;
import org.springframework.http.HttpStatus;
import org.springframework.web.bind.annotation.*;

@RestController
@RequestMapping("/api/auth")
public class AuthController {

    private final AuthService authService;

    public AuthController(AuthService authService) {
        this.authService = authService;
    }

    @PostMapping("/register")
    @ResponseStatus(HttpStatus.CREATED)
    public String register(
            @Valid @RequestBody RegisterRequest request) {
        return authService.register(request);
    }

    @PostMapping("/login")
    public String login(
            @Valid @RequestBody LoginRequest request) {
        return authService.login(request);
    }
}
