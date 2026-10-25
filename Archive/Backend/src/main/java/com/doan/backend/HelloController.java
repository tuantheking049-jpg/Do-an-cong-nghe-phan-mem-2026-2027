package com.doan.backend;

import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.RestController;
import javax.sql.DataSource;
import java.sql.Connection;
import java.sql.SQLException;

@RestController
public class HelloController {

    private final DataSource dataSource;

    public HelloController(DataSource dataSource) {
        this.dataSource = dataSource;
    }

    @GetMapping("/api/hello")
    public String hello() {
        return "Backend is running successfully!";
    }

    @GetMapping("/api/db-check")
    public String checkDatabase() {
        try (Connection connection = dataSource.getConnection()) {
            return "SQL Server connected successfully!";
        } catch (SQLException e) {
            return "Database connection failed: " + e.getMessage();
        }
    }
}