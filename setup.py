from setuptools import find_packages, setup


with open("README.md") as f:
	long_description = f.read()


setup(
	name="trueface_integration",
	version="0.1.0",
	description="TrueFace 3000 biometric attendance integration for ERPNext",
	long_description=long_description,
	long_description_content_type="text/markdown",
	author="SNRG Electricals",
	author_email="admin@snrgelectricals.com",
	packages=find_packages(),
	zip_safe=False,
	include_package_data=True,
	install_requires=[],
)
