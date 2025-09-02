set_languages("c++23")
set_project("symgen")
set_version(string.format("%d.%d.%d", 0, 0, 1))
set_symbols("debug")
set_targetdir("build")

target("symgen")
    set_kind("binary")

    add_files(
        "src/**.cpp",
        "include/**.cpp",
        "include/**.c"
    )

    add_includedirs(
        "include"
    )

    add_includedirs("src", {public = true})
    add_headerfiles("src/**.hpp")
    set_runargs({"--input-directory", "test/headers/", "--generated-directory", "generated/"})
    set_rundir("$(projectdir)/build")