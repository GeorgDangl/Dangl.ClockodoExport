# Dangl.ClockodoExport

[![Actions Status](https://github.com/GeorgDangl/Dangl.ClockodoExport/workflows/continuous/badge.svg?branch=develop)](https://github.com/GeorgDangl/Dangl.ClockodoExport/actions)  

This is a small CLI tool that exports all data from your Clockodo account.

## CLI Usage

Simply execute the converter, e.g. on Windows:

    Dangl.ClockodoExport.exe -t <ApiToken> -u <UserEmail> -f <OutputFolder>

The data will be exported to a new folder generated in the `OutputFolder`. If no folder is specified, it's placed relative to the executable.

## Downloads & Documentation

[You can download the binaries directly from Dangl**Docu**](https://docs.dangl-it.com/Projects/Dangl.ClockodoExport).

### Markdown documentation

`BuildDocumentation` also creates Markdown documentation in `output/docs-markdown`, with an adjacent ZIP archive. Each bundle starts at `index.md` and contains the same articles and resources selected for the corresponding HTML site, plus Markdown API reference files where applicable. The existing documentation upload publishes the Markdown archive to DanglDocu for the same version, making it available to AI agents through the DanglDocu MCP server.
