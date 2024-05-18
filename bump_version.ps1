param (
    [parameter(Mandatory)]
    [String]$version,

    [parameter()]
    [String]$message
)

$version = "v$version"

if ($PSBoundParameters.ContainsKey('message'))
{
    git commit -am "Bumped version ``$version```n$message"
}
else
{
    git commit -am "Bumped version ``$version``"
}

git tag $version
git push
git push --tags
