#!/bin/zsh

SCRIPT_DIR="${0:A:h}"
cd "$SCRIPT_DIR" || exit 1

(
  sleep 1
  open http://127.0.0.1:5173/
) &

npm run dev -- --host 127.0.0.1
