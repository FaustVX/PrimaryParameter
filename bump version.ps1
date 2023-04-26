$v = $args[0]
git commit -am "Bumped version ``$v``"
git push
git tag v$v
git push --tags