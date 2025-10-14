# Contributing to ZLGetCert

Thank you for your interest in contributing to ZLGetCert! This document provides guidelines for contributing to the project.

## Code of Conduct

This project follows a code of conduct to ensure a welcoming environment for all contributors. Please be respectful and constructive in all interactions.

## How to Contribute

### Reporting Issues

Before creating an issue, please:
1. Search existing issues to avoid duplicates
2. Use the issue templates when available
3. Provide detailed information including:
   - Operating system and version
   - .NET Framework version
   - Steps to reproduce
   - Expected vs actual behavior
   - Relevant log files (if applicable)

### Suggesting Features

We welcome feature suggestions! Please:
1. Check existing issues and roadmap first
2. Describe the use case and benefit
3. Consider enterprise/OT environment compatibility
4. Think about backward compatibility

### Pull Requests

We welcome pull requests! Please follow these guidelines:

## Development Setup

### Prerequisites
- Windows 10/11 or Windows Server 2016+
- Visual Studio 2019 or later (or VS Code with C# extension)
- .NET Framework 4.8 Developer Pack
- Git

### Getting Started
1. Fork the repository
2. Clone your fork:
   ```bash
   git clone https://github.com/YOUR_USERNAME/ZLGetCert.git
   cd ZLGetCert
   ```
3. Open `ZLGetCert.sln` in Visual Studio
4. Build the solution to ensure everything works

## Commit Message Convention

To generate better release notes, please follow this commit message convention:

### Format
```
<type>: <description>

[optional body]

[optional footer(s)]
```

### Types
- **feat**: A new feature
- **fix**: A bug fix
- **docs**: Documentation only changes
- **style**: Changes that do not affect the meaning of the code (white-space, formatting, missing semi-colons, etc)
- **refactor**: A code change that neither fixes a bug nor adds a feature
- **perf**: A code change that improves performance
- **test**: Adding missing tests or correcting existing tests
- **chore**: Changes to the build process or auxiliary tools and libraries

### Examples

#### Good Commit Messages
```
feat: add JSON validator to configuration editor
fix: resolve radio button alignment issue in certificate type selection
docs: update README with installation instructions
refactor: improve error handling in certificate service
perf: optimize certificate generation process
test: add unit tests for configuration validation
chore: update .gitignore to exclude build artifacts
```

#### Bad Commit Messages
```
fixed stuff
updated code
changes
WIP
```

### Release Notes Generation

The automated release notes will categorize your commits based on the type prefix:

- **🚀 New Features**: `feat:`, `feature:`, `add:`, `new:`
- **🐛 Bug Fixes**: `fix:`, `bug:`, `issue:`
- **🔧 Improvements**: `improve:`, `enhance:`, `update:`, `refactor:`
- **📝 Documentation**: `docs:`, `doc:`, `readme:`

## Development Workflow

1. **Fork the repository**
2. **Create a feature branch**: `git checkout -b feature/amazing-feature`
3. **Make your changes** following the coding standards below
4. **Write good commit messages** using the convention above
5. **Test your changes** thoroughly
6. **Update documentation** if needed
7. **Push to your fork**: `git push origin feature/amazing-feature`
8. **Create a Pull Request**

## Coding Standards

### General Guidelines
- Follow the existing code style and patterns
- Use meaningful variable and method names
- Add XML documentation comments for public APIs
- Keep methods focused and reasonably sized
- Add appropriate error handling and logging

### C# Standards
- Use `var` when the type is obvious
- Use `string.Empty` instead of `""`
- Use null-conditional operators when appropriate
- Follow C# naming conventions (PascalCase for public, camelCase for private)
- Use `SecureString` for password handling

### WPF Standards
- Follow MVVM pattern
- Use data binding instead of code-behind when possible
- Keep XAML clean and readable
- Use styles and resources for consistent theming
- Add proper converters for data binding

### Security Considerations
- Never log sensitive information (passwords, private keys)
- Use `SecureString` for password handling
- Validate all user inputs
- Sanitize file paths and command arguments
- Follow principle of least privilege

### Testing
- Test your changes thoroughly
- Test in different environments (Windows Server, different .NET versions)
- Test with different certificate types and configurations
- Verify UI changes work at different DPI settings
- Test error conditions and edge cases

## Project Structure

```
ZLGetCert/
├── Models/           # Data models and entities
├── ViewModels/      # MVVM ViewModels
├── Views/           # WPF XAML views
├── Services/        # Business logic services
├── Utilities/       # Helper classes
├── Enums/           # Enumerations
├── Styles/          # XAML styles and templates
├── Converters/      # Value converters for data binding
├── Fonts/           # Font Awesome icon fonts
└── docs/            # Documentation
    ├── development/ # Technical documentation
    └── user-guides/ # User-facing documentation
```

## Pull Request Guidelines

### Before Submitting
- [ ] Code follows the project's coding standards
- [ ] All existing tests pass
- [ ] New functionality is tested
- [ ] Documentation is updated
- [ ] Commit messages follow the convention
- [ ] No sensitive information is included

### PR Description
- Use a clear, descriptive title
- Reference any related issues
- Include screenshots for UI changes
- Describe the changes and their impact
- List any breaking changes
- Update documentation as needed

### Review Process
- All PRs require review before merging
- Address review comments promptly
- Keep PRs focused and reasonably sized
- Rebase on main branch if needed

## Areas for Contribution

### High Priority
- **Bug fixes** - Any issues reported by users
- **Documentation** - Improve user guides and API docs
- **Testing** - Add unit tests and integration tests
- **Performance** - Optimize certificate generation and UI responsiveness

### Medium Priority
- **New certificate types** - Additional certificate templates
- **UI improvements** - Enhanced user experience
- **Configuration** - Additional configuration options
- **Logging** - Enhanced logging and debugging

### Low Priority
- **Internationalization** - Multi-language support
- **Themes** - Dark mode or custom themes
- **Plugins** - Plugin architecture for extensions

## Enterprise Considerations

When contributing, please consider:
- **Legacy compatibility** - Maintain support for older Windows versions
- **Air-gapped environments** - Avoid external dependencies
- **OT/SCADA compatibility** - Consider industrial control systems
- **Security** - Follow enterprise security practices
- **Performance** - Optimize for resource-constrained environments

## Development Tools

### Recommended
- **Visual Studio 2019+** - Full IDE with debugging
- **VS Code** - Lightweight editor with C# extension
- **Git for Windows** - Version control
- **Windows Terminal** - Modern terminal

### Useful Extensions
- C# for Visual Studio Code
- WPF Designer (Visual Studio)
- GitLens (VS Code)
- PowerShell (VS Code)

## Getting Help

If you need help:
1. Check the [documentation](docs/)
2. Search existing [issues](https://github.com/ZentrixLabs/ZLGetCert/issues)
3. Create a new issue with detailed information
4. Join discussions in issues or pull requests

## Recognition

Contributors will be recognized in:
- Release notes
- CONTRIBUTORS.md file
- GitHub contributor graph
- Project documentation

## Questions?

If you have any questions about contributing, please:
- Open an issue with the "question" label
- Contact the maintainers
- Join the discussion in existing issues

Thank you for contributing to ZLGetCert! 🎉

---

**Note**: This project is designed for enterprise and OT environments. Please consider the security, compatibility, and reliability requirements of these environments when contributing.