# Deployment Guide

## Linux VPS requirements

- Ubuntu 22.04 or 24.04 LTS
- Docker Engine and Docker Compose
- Nginx or Caddy for TLS termination
- Docker-managed logs and restart policies
- Automated patching and monitoring

## Neon configuration

- Use a Neon PostgreSQL instance with SSL enabled.
- Store the connection string in environment variables or secrets manager.
- Do not expose PostgreSQL directly to the internet.
- Use a pooled connection string for the web API and a direct connection string only for migrations if required.

## Secret management

Store secrets in `.env` files only during local development and in a secure secrets manager or systemd environment in production.

## Docker setup

Use the included Dockerfile and Compose file with environment variables populated at deployment time.

## HTTPS

Place the API behind Nginx or Caddy with TLS certificates issued by Let’s Encrypt or an internal CA.

## Migration deployment

Run the EF Core migration before starting the API, using a controlled release pipeline and a maintenance window.

## Backup and recovery

- Use Neon backups and point-in-time recovery.
- Keep application logs and object storage backups separate.
- Verify restore steps in a staging environment before production drills.
