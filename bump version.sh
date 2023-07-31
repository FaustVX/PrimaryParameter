#!/bin/bash
git commit -am "Bumped version \`$1\`"
git push
git tag "$1"
git push --tags
