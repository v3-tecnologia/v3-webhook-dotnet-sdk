# Contributing to v3-webhook-dotnet-sdk

First of all, thank you for your interest in contributing to **V3 Webhook Go SDK**!\
Contributions of all kinds are welcome --- bug fixes, improvements,
documentation, new event types, and examples.

This project is used with **real production IoT events**, so
correctness, stability, and backward compatibility are extremely
important.

------------------------------------------------------------------------

## 📋 Table of Contents

-   [Code of Conduct](#code-of-conduct)
-   [How Can I Contribute?](#how-can-i-contribute)
    -   [Reporting Bugs](#reporting-bugs)
    -   [Suggesting Enhancements](#suggesting-enhancements)
    -   [Adding New Event Types](#adding-new-event-types)
    -   [Improving Documentation](#improving-documentation)
-   [Development Guidelines](#development-guidelines)
    -   [Project Structure](#project-structure)
    -   [Coding Style](#coding-style)
    -   [Strong Typing & Safety](#strong-typing--safety)
    -   [Backward Compatibility](#backward-compatibility)
-   [Testing](#testing)
-   [Commit Guidelines](#commit-guidelines)
-   [Pull Request Process](#pull-request-process)
-   [License](#license)

------------------------------------------------------------------------

## 📜 Code of Conduct

This project follows a simple rule: **be respectful and constructive**.

Harassment, offensive language, or unprofessional behavior will not be
tolerated.\
By participating, you agree to maintain a welcoming and collaborative
environment.

------------------------------------------------------------------------

## 🤝 How Can I Contribute?

### 🐛 Reporting Bugs

If you find a bug:

1.  Check if it was already reported in the **Issues**.
2.  If not, open a new issue including:
    -   A clear and descriptive title
    -   The event payload (JSON) that caused the issue (anonymized if
        needed)
    -   Expected behavior
    -   Actual behavior
    -   Go version and library version

**Tip:** Real event payloads (sanitized) are extremely helpful.

------------------------------------------------------------------------

### 💡 Suggesting Enhancements

Enhancement ideas are welcome, especially when related to:

-   New event categories or subtypes
-   Better handler ergonomics
-   Performance improvements
-   Validation or safety improvements
-   Developer experience (DX)

Please include: - Motivation / problem being solved - Example usage -
Backward compatibility considerations

------------------------------------------------------------------------

### 📦 Adding New Event Types

Adding new event types is a **common and encouraged contribution**, but
please follow these rules:

1.  **Use real or realistic payloads**

2.  Add types under the correct package:

        pkg/types/<category>/

3.  Follow existing naming conventions:

    -   `Event`
    -   `GetXxxData()` helpers

4.  Update:

    -   Event enums (`EventCategory`, `EventSub`, etc.)
    -   Handlers and callbacks if applicable

5.  Add at least one **example JSON payload**

6.  Add or update tests

> ⚠️ Avoid breaking existing event mappings unless strictly necessary.

------------------------------------------------------------------------

### 📚 Improving Documentation

You can contribute by: - Fixing typos - Improving explanations - Adding
examples - Clarifying handler behavior - Updating README sections

Documentation should always reflect **real behavior**, not aspirational
features.

------------------------------------------------------------------------

## 🛠 Development Guidelines

### 📂 Project Structure

Please respect the existing structure:

-   `pkg/types/*` → Strongly-typed event models
-   `pkg/webhook` → Event processor, handlers, builders
-   `examples/` → Runnable examples
-   `event-mapping-viewer/` → JSON payload references

Do not introduce cross-category coupling between event types.

------------------------------------------------------------------------

### ✍️ Coding Style

-   Follow standard Go conventions (`gofmt` is mandatory)
-   Prefer explicit code over clever code
-   Avoid unnecessary abstractions
-   Keep functions small and focused
-   Public APIs must be documented

------------------------------------------------------------------------

### 🧱 Strong Typing & Safety

This library prioritizes: - Strong typing - Null safety - Explicit
conversions

Rules: - Avoid `interface{}` in public APIs - Use helper methods instead
of direct struct access - Always validate event category and subtype
before casting

------------------------------------------------------------------------

### 🔁 Backward Compatibility

**Backward compatibility is critical.**

-   Do not rename exported types or methods
-   Do not change JSON mappings without discussion
-   If behavior must change, document it clearly

Breaking changes require: - Clear justification - Maintainer approval -
Proper versioning discussion

------------------------------------------------------------------------

## 🧪 Testing

Before submitting a PR:

-   Add tests for new functionality
-   Ensure existing tests pass
-   Validate with real JSON payloads when possible

Recommended:

``` bash
go test ./...
```

If adding new event types, test at least: - Parsing - Handler
invocation - Helper methods

------------------------------------------------------------------------

## 📝 Commit Guidelines

Use clear and descriptive commit messages:

**Good examples:**

    feat: add support for DMS yawning event
    fix: handle nil telemetry metrics safely
    docs: improve webhook usage examples

Avoid:

    fix stuff
    update code
    changes

------------------------------------------------------------------------

## 🔀 Pull Request Process

1.  Fork the repository

2.  Create a feature branch:

    ``` bash
    git checkout -b feature/my-feature
    ```

3.  Commit your changes

4.  Push to your fork

5.  Open a Pull Request

Your PR should include: - Clear description of the change - Motivation
and context - References to issues (if any) - Notes about backward
compatibility

All PRs are reviewed before merging.

------------------------------------------------------------------------

## 📄 License

By contributing, you agree that your contributions will be licensed
under the **MIT License**, the same license as this project.

------------------------------------------------------------------------

## 🙌 Thank You

Thank you for helping improve **v3-webhook-dotnet-sdk**!\
Your contributions help make IoT event processing safer, cleaner, and
easier for everyone.