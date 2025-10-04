# Contributing to ZLGetCert

Thank you for your interest in contributing to ZLGetCert! This document provides guidelines for contributing to the project.

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
perf: optimize OpenSSL process execution
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
3. **Make your changes** following the coding standards
4. **Write good commit messages** using the convention above
5. **Test your changes** thoroughly
6. **Push to your fork**: `git push origin feature/amazing-feature`
7. **Create a Pull Request**

## Coding Standards

- Follow the existing code style and patterns
- Add appropriate error handling and logging
- Update documentation for new features
- Test your changes thoroughly
- Ensure all existing tests pass

## Pull Request Guidelines

- Use a clear, descriptive title
- Reference any related issues
- Include screenshots for UI changes
- Update documentation as needed
- Ensure the build passes

## Questions?

If you have any questions about contributing, please open an issue or contact the maintainers.

Thank you for contributing to ZLGetCert! 🎉
