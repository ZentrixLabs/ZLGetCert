# 🧭 Start Here: Structure Standard

ZLGetCert is being refactored under the **BakBeatLabs Structure Standard**.

This standard defines the authoritative behavior of the tool and takes precedence over all other documentation.

Before reading user guides, UX notes, or development history, start here:

- **docs/Structure-Standard.md**

That document links to four required anchors:

1. **Contract** — what ZLGetCert is allowed to do  
2. **Doctor** — read-only preflight that blocks unsafe runs  
3. **CLI** — the canonical reference interface  
4. **Fixtures** — proof artifacts that enforce behavior  

### Authority Rule

If there is any conflict between:
- UI behavior
- User guides
- Development notes
- Implementation details

**The Structure Standard wins.**

The documentation below remains valuable context, but it must not override the Structure Standard.

---

# ZLGetCert Documentation

Welcome to the ZLGetCert documentation! This directory contains comprehensive documentation for users and developers.

## 📚 Documentation Structure

### User Guides
The [`user-guides/`](user-guides/) directory contains feature-specific documentation and improvement guides:

- **[CSR Workflow Improvements](user-guides/CSR_WORKFLOW_IMPROVEMENTS.md)** - Certificate Signing Request workflow enhancements
- **[Form Validation Improvements](user-guides/FORM_VALIDATION_IMPROVEMENTS.md)** - Real-time form validation and error handling
- **[FQDN Auto-Generation](user-guides/FQDN_AUTO_GENERATION_IMPROVEMENT.md)** - Automatic FQDN generation from hostname
- **[Organization Fields Context](user-guides/ORGANIZATION_FIELDS_CONTEXT_IMPROVEMENT.md)** - Enhanced organization field guidance
- **[Password UX Improvements](user-guides/PASSWORD_UX_IMPROVEMENTS.md)** - Password handling and security features
- **[SAN Management](user-guides/SAN_MANAGEMENT_BULK_ENTRY_IMPROVEMENT.md)** - Subject Alternative Names bulk entry
- **[Security Warning Improvements](user-guides/SECURITY_WARNING_IMPROVEMENT.md)** - Enhanced security guidance
- **[Status Message Placement](user-guides/STATUS_MESSAGE_PLACEMENT_IMPROVEMENT.md)** - Improved status messaging
- **[Template Selection Improvements](user-guides/TEMPLATE_SELECTION_IMPROVEMENT.md)** - Better template selection UX
- **[Unencrypted Key Security Guidance](user-guides/UNENCRYPTED_KEY_SECURITY_GUIDANCE.md)** - Security best practices
- **[UX Improvements Session](user-guides/UX_IMPROVEMENTS_SESSION_OCT_14_2025.md)** - Comprehensive UX improvements
- **[Final UX Improvements Summary](user-guides/FINAL_UX_IMPROVEMENTS_SUMMARY.md)** - Complete UX enhancement overview

### Development Documentation
The [`development/`](development/) directory contains technical implementation details:

- **[Icon Standardization Implementation](development/ICON_STANDARDIZATION_IMPLEMENTATION.md)** - Font Awesome integration guide
- **[Icon Standardization Summary](development/ICON_STANDARDIZATION_SUMMARY.md)** - Icon system overview
- **[Visual Consistency & Branding](development/VISUAL_CONSISTENCY_BRANDING_COMPLETE.md)** - Branding implementation
- **[UX Review Recommendations](development/UX_REVIEW_RECOMMENDATIONS.md)** - Complete UX assessment
- **[Work Completed Summary](development/WORK_COMPLETED_SUMMARY.md)** - Development progress tracking
- **[Future Enhancement DPAPI Encryption](development/FUTURE_ENHANCEMENT_DPAPI_ENCRYPTION.md)** - Future security enhancements
- **[Template Parsing Fix](development/TEMPLATE_PARSING_FIX.md)** - Technical bug fixes

## 🚀 Quick Start

### For Users
1. Read the main [README.md](../README.md) for installation and basic usage
2. Check the [user-guides](user-guides/) for specific features
3. Review [configuration examples](../ZLGetCert/examples/) for setup

### For Developers
1. Read [CONTRIBUTING.md](../CONTRIBUTING.md) for contribution guidelines
2. Review [development documentation](development/) for technical details
3. Check the [project structure](../README.md#development) for code organization

## 📖 Additional Resources

- **[Configuration Examples](../ZLGetCert/examples/)** - Sample configuration files
- **[Reference Scripts](../reference/)** - PowerShell reference implementations
- **[Main README](../README.md)** - Project overview and installation
- **[Contributing Guidelines](../CONTRIBUTING.md)** - How to contribute

## 🔍 Finding Information

### By Topic
- **Installation & Setup**: [Main README](../README.md#installation)
- **Configuration**: [Main README](../README.md#configuration) + [examples](../ZLGetCert/examples/)
- **UI Features**: [user-guides](user-guides/) directory
- **Development**: [development](development/) directory
- **Contributing**: [CONTRIBUTING.md](../CONTRIBUTING.md)

### By User Type
- **End Users**: Start with [Main README](../README.md) → [user-guides](user-guides/)
- **System Administrators**: [Main README](../README.md) → [configuration examples](../ZLGetCert/examples/)
- **Developers**: [CONTRIBUTING.md](../CONTRIBUTING.md) → [development docs](development/)
- **Contributors**: [CONTRIBUTING.md](../CONTRIBUTING.md) → [development docs](development/)

## 📝 Documentation Standards

All documentation follows these standards:
- **Clear headings** with descriptive titles
- **Code examples** with syntax highlighting
- **Step-by-step instructions** where applicable
- **Cross-references** to related documentation
- **Regular updates** with code changes

## 🤝 Contributing to Documentation

Documentation improvements are welcome! Please:
1. Follow the [contributing guidelines](../CONTRIBUTING.md)
2. Use clear, concise language
3. Include code examples where helpful
4. Update the index when adding new documents
5. Test any instructions or examples

## 📞 Support

For documentation questions:
- Check existing documentation first
- Search [GitHub issues](https://github.com/ZentrixLabs/ZLGetCert/issues)
- Create a new issue with the "documentation" label
- Include specific page/section references

---

**Last Updated**: October 14, 2025  
**Version**: 1.0.0
