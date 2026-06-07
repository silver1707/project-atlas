#!/usr/bin/env bash
set -euo pipefail

: "${PGHOST:=postgres}"
: "${PGPORT:=5432}"
: "${PGDATABASE:=autoparts_erp}"
: "${PGUSER:=autoparts}"
: "${BACKUP_DIR:=./backups/base}"

mkdir -p "${BACKUP_DIR}"
pg_basebackup \
  --host="${PGHOST}" \
  --port="${PGPORT}" \
  --username="${PGUSER}" \
  --pgdata="${BACKUP_DIR}/$(date -u +%Y%m%dT%H%M%SZ)" \
  --format=plain \
  --wal-method=stream \
  --checkpoint=fast \
  --progress
