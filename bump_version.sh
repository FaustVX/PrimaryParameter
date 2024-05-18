#!/bin/bash
version=$1
message=$2

version="v$version"

if [ $# -eq 1 ]; then
    git commit -am "Bumped version \`$version\`"
elif [ $# -eq 2 ]; then
    git commit -am "Bumped version \`$version\`\n$message"
else
    echo "Exactly 1 or 2 argument must be provided"
    exit false
fi

git push
git tag "$version"
git push --tags
