$v = $args[0]
git commit -am "Bumped version ``$v``"
git push
git tag $v
git push --tags