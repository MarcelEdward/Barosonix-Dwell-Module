DROP TABLE IF EXISTS `landDwell`;
CREATE TABLE `landDwell` (
  `id` char(36) NOT NULL,
  `pid` char(36) NOT NULL,
  `timestamp` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  `idkey` int(20) NOT NULL AUTO_INCREMENT,
  `avatar_name` varchar(128) NOT NULL,
  `parcel_name` varchar(255) NOT NULL,
  `region_uuid` varchar(45) DEFAULT NULL,
  `region_name` varchar(255) DEFAULT NULL,
  `localLandID` int(20) DEFAULT NULL,
  `parcelOwner` varchar(36) DEFAULT NULL,
  `parcelGroupOwned` int(1) DEFAULT NULL,
  `parcelOwnerName` varchar(128) DEFAULT NULL,
  PRIMARY KEY (`idkey`),
  UNIQUE KEY `id_UNIQUE` (`idkey`),
  KEY `pid_timestamp` (`pid`,`timestamp`),
  KEY `id` (`id`),
  KEY `pid` (`pid`),
  KEY `id_pid` (`pid`,`id`),
  KEY `ParcelOwner` (`parcelOwner`)
) ENGINE=MyISAM AUTO_INCREMENT=215 DEFAULT CHARSET=latin1;
