import { NgModule } from '@angular/core';
import { SharedModule } from '@shared/shared.module';
import { CommonModule } from '@angular/common';
import { CampaignsRoutingModule } from './campaigns-routing.module';
import { CampaignsComponent } from './campaigns.component';
import { CreateCampaignDialogComponent } from './create-campaign/create-campaign-dialog.component';
import { EditCampaignDialogComponent } from './edit-campaign/edit-campaign-dialog.component';

@NgModule({
    imports: [
        SharedModule,
        CommonModule,
        CampaignsRoutingModule,
        CampaignsComponent,
        CreateCampaignDialogComponent,
        EditCampaignDialogComponent,
    ],
})
export class CampaignsModule {}
