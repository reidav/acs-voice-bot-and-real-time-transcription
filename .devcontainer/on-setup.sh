#!/usr/bin/env bash

set -e

set_tools() {
    sudo apt-get update
    curl -sL https://aka.ms/DevTunnelCliInstall | bash
}

# Install tools
set_tools