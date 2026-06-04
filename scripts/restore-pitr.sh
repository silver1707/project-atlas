#!/usr/bin/env bash
set -euo pipefail

if [ "$#" -lt 3 ]; then
  echo "usage: restore-pitr.sh <base-backup-dir> <wal-archive-dir> <recovery-target-time-utc>"
  exit 64
fi

BASE_BACKUP_DIR="$1"
WAL_ARCHIVE_DIR="$2"
TARGET_TIME="$3"
: "${PGDATA:=/var/lib/postgresql/data}"

rm -rf "${PGDATA:?}"/*
cp -a "${BASE_BACKUP_DIR}/." "${PGDATA}/"
cat > "${PGDATA}/postgresql.auto.conf" <<EOF
restore_command = 'cp ${WAL_ARCHIVE_DIR}/%f %p'
recovery_target_time = '${TARGET_TIME}'
recovery_target_action = 'promote'
EOF
touch "${PGDATA}/recovery.signal"
echo "PITR configured for ${TARGET_TIME}. Start PostgreSQL to recover."
